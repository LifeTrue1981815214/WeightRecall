using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Plugin.LocalNotification;
using WeightRecall.Data;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.Views;

internal sealed class ExportData
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public List<RoutineItemExport> RoutineItems { get; set; } = [];
    public List<WorkoutLogExport> WorkoutLogs { get; set; } = [];
}

internal sealed class RoutineItemExport
{
    public string ExerciseName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public int Order { get; set; }
}

internal sealed class WorkoutLogExport
{
    public DateTime Date { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double Weight { get; set; }
}

public partial class SettingsPage : ContentPage
{
    private readonly NotificationService _notificationService;
    private readonly DatabaseContext _databaseContext;
    private static readonly string DbPath = Path.Combine(
        FileSystem.AppDataDirectory,
        "WeightRecall.db3"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public SettingsPage(NotificationService notificationService, DatabaseContext databaseContext)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _databaseContext = databaseContext;

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
                "JSON (.json)" => await ExportAsJsonAsync(),
                "Text (.txt)" => await ExportAsTxtAsync(),
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

    private async Task<string> ExportAsJsonAsync()
    {
        ExportData data = await BuildExportDataAsync();
        string json = JsonSerializer.Serialize(data, JsonOptions);
        string path = GetTempExportPath("json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    private async Task<string> ExportAsTxtAsync()
    {
        ExportData data = await BuildExportDataAsync();
        StringBuilder sb = new();

        sb.AppendLine("[RoutineItems]");
        sb.AppendLine("ExerciseName,DayOfWeek,Order");
        foreach (RoutineItemExport r in data.RoutineItems)
        {
            sb.AppendLine($"{CsvEscape(r.ExerciseName)},{r.DayOfWeek},{r.Order}");
        }

        sb.AppendLine();
        sb.AppendLine("[WorkoutLogs]");
        sb.AppendLine("Date,ExerciseName,Sets,Reps,Weight");
        foreach (WorkoutLogExport w in data.WorkoutLogs)
        {
            sb.AppendLine(
                FormattableString.Invariant(
                    $"{w.Date:O},{CsvEscape(w.ExerciseName)},{w.Sets},{w.Reps},{w.Weight}"
                )
            );
        }

        string path = GetTempExportPath("txt");
        await File.WriteAllTextAsync(path, sb.ToString());
        return path;
    }

    private async Task<ExportData> BuildExportDataAsync()
    {
        await _databaseContext.InitializeAsync();
        List<RoutineItem> routineItems = await _databaseContext
            .Connection.Table<RoutineItem>()
            .ToListAsync();
        List<WorkoutLog> workoutLogs = await _databaseContext
            .Connection.Table<WorkoutLog>()
            .ToListAsync();

        return new ExportData
        {
            ExportedAt = DateTime.UtcNow,
            RoutineItems = routineItems
                .Select(r => new RoutineItemExport
                {
                    ExerciseName = r.ExerciseName,
                    DayOfWeek = r.DayOfWeek,
                    Order = r.Order,
                })
                .ToList(),
            WorkoutLogs = workoutLogs
                .Select(w => new WorkoutLogExport
                {
                    Date = w.Date,
                    ExerciseName = w.ExerciseName,
                    Sets = w.Sets,
                    Reps = w.Reps,
                    Weight = w.Weight,
                })
                .ToList(),
        };
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
                    await ImportFromJsonAsync(result);
                    break;
                case ".txt":
                    await ImportFromTxtAsync(result);
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

    private async Task ImportFromJsonAsync(FileResult result)
    {
        ExportData? data;
        using (Stream stream = await result.OpenReadAsync())
        {
            data = await JsonSerializer.DeserializeAsync<ExportData>(stream, JsonOptions);
        }

        if (data is null)
        {
            throw new InvalidDataException("Invalid or empty JSON file.");
        }

        await ApplyImportDataAsync(data);
    }

    private async Task ImportFromTxtAsync(FileResult result)
    {
        string content;
        using (Stream stream = await result.OpenReadAsync())
        using (StreamReader reader = new(stream))
        {
            content = await reader.ReadToEndAsync();
        }

        ExportData data = ParseTxt(content);
        await ApplyImportDataAsync(data);
    }

    private async Task ApplyImportDataAsync(ExportData data)
    {
        await _databaseContext.InitializeAsync();
        await _databaseContext.Connection.DeleteAllAsync<WorkoutLog>();
        await _databaseContext.Connection.DeleteAllAsync<RoutineItem>();

        await _databaseContext.Connection.InsertAllAsync(
            data.RoutineItems.Select(r => new RoutineItem
            {
                ExerciseName = r.ExerciseName,
                DayOfWeek = r.DayOfWeek,
                Order = r.Order,
            })
        );

        await _databaseContext.Connection.InsertAllAsync(
            data.WorkoutLogs.Select(w => new WorkoutLog
            {
                Date = w.Date,
                ExerciseName = w.ExerciseName,
                Sets = w.Sets,
                Reps = w.Reps,
                Weight = w.Weight,
            })
        );
    }

    // ── TXT parsing ──────────────────────────────────────────────────────────

    private static ExportData ParseTxt(string content)
    {
        ExportData data = new();
        string? section = null;
        bool headerSkipped = false;

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            if (line == "[RoutineItems]")
            {
                section = "RoutineItems";
                headerSkipped = false;
                continue;
            }
            if (line == "[WorkoutLogs]")
            {
                section = "WorkoutLogs";
                headerSkipped = false;
                continue;
            }

            if (!headerSkipped)
            {
                headerSkipped = true;
                continue;
            }

            string[] parts = SplitCsvLine(line);

            if (section == "RoutineItems" && parts.Length >= 3)
            {
                data.RoutineItems.Add(
                    new RoutineItemExport
                    {
                        ExerciseName = parts[0],
                        DayOfWeek = Enum.Parse<DayOfWeek>(parts[1]),
                        Order = int.Parse(parts[2], CultureInfo.InvariantCulture),
                    }
                );
            }
            else if (section == "WorkoutLogs" && parts.Length >= 5)
            {
                data.WorkoutLogs.Add(
                    new WorkoutLogExport
                    {
                        Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                        ExerciseName = parts[1],
                        Sets = int.Parse(parts[2], CultureInfo.InvariantCulture),
                        Reps = int.Parse(parts[3], CultureInfo.InvariantCulture),
                        Weight = double.Parse(parts[4], CultureInfo.InvariantCulture),
                    }
                );
            }
        }

        return data;
    }

    private static string[] SplitCsvLine(string line)
    {
        List<string> result = [];
        bool inQuotes = false;
        StringBuilder current = new();

        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return [.. result];
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
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
