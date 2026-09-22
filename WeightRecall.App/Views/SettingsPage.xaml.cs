using Plugin.LocalNotification;
using WeightRecall.Data;
using WeightRecall.Models;
using WeightRecall.Serialization;
using WeightRecall.Services;

namespace WeightRecall.Views;

public partial class SettingsPage : ContentPage
{
    private readonly NotificationService _notificationService;
    private readonly DatabaseContext _databaseContext;
    private readonly BackupService _backupService;

    private static readonly string DbPath = Path.Combine(
        FileSystem.AppDataDirectory,
        "WeightRecall.db3"
    );

    public SettingsPage(
        NotificationService notificationService,
        DatabaseContext databaseContext,
        BackupService backupService
    )
    {
        InitializeComponent();
        _notificationService = notificationService;
        _databaseContext = databaseContext;
        _backupService = backupService;

        // Set initial value before subscribing so it doesn't trigger the handler
        NotificationSwitch.IsToggled = Preferences.Default.Get("NotificationsEnabled", true);
        NotificationSwitch.Toggled += OnNotificationToggled;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        NotificationSwitch.Toggled -= OnNotificationToggled;

        bool prefEnabled = Preferences.Default.Get("NotificationsEnabled", true);
        bool systemEnabled =
            !prefEnabled || await LocalNotificationCenter.Current.AreNotificationsEnabled();
        NotificationSwitch.IsToggled = prefEnabled && systemEnabled;

        if (prefEnabled && !systemEnabled)
        {
            Preferences.Default.Set("NotificationsEnabled", false);
        }

        NotificationSwitch.Toggled += OnNotificationToggled;
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    // ── Notifications ────────────────────────────────────────────────────────

    private async void OnNotificationToggled(object? sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("NotificationsEnabled", e.Value);

        if (e.Value)
        {
            bool isAllowed = await NotificationService.RequestNotificationPermission();

            if (!isAllowed)
            {
                await DisplayAlertAsync(
                    "Permissions Required",
                    "Please grant all required permissions in your device settings to receive daily exercise reminders.",
                    "OK"
                );

                NotificationSwitch.Toggled -= OnNotificationToggled;
                NotificationSwitch.IsToggled = false;
                NotificationSwitch.Toggled += OnNotificationToggled;

                Preferences.Default.Set("NotificationsEnabled", false);
                return;
            }
        }

        await _notificationService.ScheduleDailyNotifications();
    }

    // ── Export ───────────────────────────────────────────────────────────────

    private async void OnExportClicked(object? sender, EventArgs e)
    {
        string? choice = await DisplayActionSheetAsync(
            "Export As",
            "Cancel",
            null,
            "Database (.db3)",
            "JSON (.json)",
            "Text (.txt)"
        );

        if (choice is null or "Cancel")
        {
            return;
        }

        try
        {
            string? path = choice switch
            {
                "Database (.db3)" => GetDb3ExportPath(),
                "JSON (.json)" => await WriteBackupFileAsync("json", BackupJsonFormat.Write),
                "Text (.txt)" => await WriteBackupFileAsync("txt", BackupTextFormat.Write),
                _ => null,
            };

            if (path is null)
            {
                return;
            }

            await Share.Default.RequestAsync(
                new ShareFileRequest
                {
                    Title = "Export WeightRecall Data",
                    File = new ShareFile(path),
                }
            );

            if (!path.Equals(DbPath, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(path);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Export Failed", $"Failed to export: {ex.Message}", "OK");
        }
    }

    private string? GetDb3ExportPath()
    {
        if (!File.Exists(DbPath))
        {
            _ = DisplayAlertAsync("Export Failed", "Database file not found.", "OK");
            return null;
        }
        return DbPath;
    }

    /// <summary>
    /// Writes the current data to a temporary file in the given format, and returns its path.
    /// </summary>
    /// <param name="extension">File extension to use, without the dot.</param>
    /// <param name="render">Turns the backup into the text that gets written.</param>
    private async Task<string> WriteBackupFileAsync(
        string extension,
        Func<BackupData, string> render
    )
    {
        BackupData data = await _backupService.BuildAsync();
        string path = GetTempExportPath(extension);
        await File.WriteAllTextAsync(path, render(data));
        return path;
    }

    // ── Import ───────────────────────────────────────────────────────────────

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        bool confirmed = await DisplayAlertAsync(
            "Import Data",
            "This will replace all current data. Continue?",
            "Import",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        try
        {
            FileResult? result = await FilePicker.Default.PickAsync(
                new PickOptions { PickerTitle = "Select Export File" }
            );

            if (result is null)
            {
                return;
            }

            string ext = Path.GetExtension(result.FileName).ToLowerInvariant();

            switch (ext)
            {
                case ".db3":
                    await ImportFromDb3Async(result);
                    break;
                case ".json":
                    await ImportTextFileAsync(result, BackupJsonFormat.Read);
                    break;
                case ".txt":
                    await ImportTextFileAsync(result, BackupTextFormat.Parse);
                    break;
                default:
                    await DisplayAlertAsync(
                        "Unsupported Format",
                        "Please select a .db3, .json, or .txt file.",
                        "OK"
                    );
                    return;
            }

            await DisplayAlertAsync(
                "Import Complete",
                "Data imported successfully. The app will now restart.",
                "OK"
            );

            RestartApp();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Import Failed", $"Failed to import: {ex.Message}", "OK");
        }
    }

    /// <summary>
    /// Reads a picked text file, parses it with the given format, and restores it.
    /// </summary>
    /// <param name="result">The file the user picked.</param>
    /// <param name="parse">Turns the file's text into a backup payload.</param>
    private async Task ImportTextFileAsync(FileResult result, Func<string, BackupData?> parse)
    {
        string content;
        using (Stream stream = await result.OpenReadAsync())
        using (StreamReader reader = new(stream))
        {
            content = await reader.ReadToEndAsync();
        }

        BackupData data =
            parse(content) ?? throw new InvalidDataException("Invalid or empty backup file.");

        await _backupService.ApplyAsync(data);
    }

    private async Task ImportFromDb3Async(FileResult result)
    {
        string tempPath = Path.Combine(FileSystem.CacheDirectory, "import_temp.db3");

        using (Stream source = await result.OpenReadAsync())
        using (FileStream dest = File.Create(tempPath))
        {
            await source.CopyToAsync(dest);
        }

        await _databaseContext.Connection.CloseAsync();
        File.Copy(tempPath, DbPath, overwrite: true);
        File.Delete(tempPath);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetTempExportPath(string extension)
    {
        return Path.Combine(
            FileSystem.CacheDirectory,
            $"WeightRecall_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}"
        );
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        { /* ignore cleanup errors */
        }
    }

    private static void RestartApp()
    {
        Android.Content.Context context = Android.App.Application.Context;
        Android.Content.Intent? intent = context.PackageManager?.GetLaunchIntentForPackage(
            context.PackageName ?? string.Empty
        );

        if (intent is not null)
        {
            intent.AddFlags(
                Android.Content.ActivityFlags.NewTask | Android.Content.ActivityFlags.ClearTask
            );
            context.StartActivity(intent);
        }

        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
    }
}
