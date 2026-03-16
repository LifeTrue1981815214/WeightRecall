using System.Text;
using System.Text.Json;
using WeightRecall.Models;
using WeightRecall.Repository;
using WeightRecall.Services;
using WeightRecall.Views;

namespace WeightRecall;

/// <summary>
/// Interaction logic for the application shell, managing navigation and settings common to all pages.
/// </summary>
public partial class AppShell : Shell
{
    private readonly NotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// Configures routing and initializes UI state from preferences.
    /// </summary>
    /// <param name="notificationService">Service for managing notifications.</param>
    public AppShell(NotificationService notificationService)
    {
        InitializeComponent();
        _notificationService = notificationService;

        Routing.RegisterRoute(nameof(ProgressPage), typeof(ProgressPage));

        NotificationSwitch.IsToggled = Preferences.Default.Get("NotificationsEnabled", true);
    }

    /// <summary>
    /// Event handler for the notification toggle switch.
    /// Updates preferences and schedules or cancels notifications accordingly.
    /// </summary>
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
