using WeightRecall.Services;

namespace WeightRecall;

public partial class AppShell : Shell
{
    private readonly NotificationService _notificationService;

    public AppShell(NotificationService notificationService)
    {
        InitializeComponent();
        _notificationService = notificationService;

        Routing.RegisterRoute(nameof(ProgressPage), typeof(ProgressPage));

        NotificationSwitch.IsToggled = Preferences.Default.Get("NotificationsEnabled", true);
    }

    private async void OnNotificationToggled(object? sender, ToggledEventArgs e)
    {
        Preferences.Default.Set("NotificationsEnabled", e.Value);

        if (e.Value)
        {
            bool isAllowed = await _notificationService.RequestNotificationPermission();

            if (!isAllowed)
            {
                await DisplayAlertAsync(
                    "Notifications Disabled",
                    "Please enable notifications in your device settings to receive daily exercise reminders.",
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
}
