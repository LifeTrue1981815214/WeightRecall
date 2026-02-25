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

        // Set initial state based on preferences
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
                // If the user declined or it's blocked in settings, show an alert and turn the switch back off
                await DisplayAlertAsync(
                    "Notifications Disabled",
                    "Please enable notifications in your device settings to receive daily exercise reminders.",
                    "OK"
                );

                // Temporarily detach to prevent re-triggering logic when we reset the switch
                NotificationSwitch.Toggled -= OnNotificationToggled;
                NotificationSwitch.IsToggled = false;
                NotificationSwitch.Toggled += OnNotificationToggled;

                // Ensure preference is also reset
                Preferences.Default.Set("NotificationsEnabled", false);
                return;
            }
        }

        await _notificationService.ScheduleDailyNotifications();
    }
}
