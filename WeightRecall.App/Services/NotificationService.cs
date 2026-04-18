using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

public class NotificationService(
    RoutineRepository routineRepository,
    ILogger<NotificationService> logger
) : IWorkoutNotificationService
{
    private readonly RoutineRepository _routineRepository = routineRepository;
    private readonly ILogger<NotificationService> _logger = logger;

    /// <summary>
    /// Requests POST_NOTIFICATIONS permission and, on Android 12+, SCHEDULE_EXACT_ALARM.
    /// Opens system settings if the exact alarm permission is missing.
    /// </summary>
    public static async Task<bool> RequestNotificationPermission()
    {
        if (!await LocalNotificationCenter.Current.RequestNotificationPermission())
        {
            return false;
        }

        // Android 12+ requires a separate permission to fire alarms at an exact time
        if (OperatingSystem.IsAndroidVersionAtLeast(31) && !CanScheduleExactAlarms())
        {
            OpenExactAlarmSettings();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Cancels all existing notifications, then schedules a weekly 10 AM notification
    /// for every day that has exercises in the routine.
    /// Does nothing if notifications are disabled in preferences or system settings.
    /// </summary>
    public async Task ScheduleDailyNotifications()
    {
        try
        {
            _ = LocalNotificationCenter.Current.CancelAll();

            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                return;
            }

            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                return;
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(31) && !CanScheduleExactAlarms())
            {
                return;
            }

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                List<RoutineItem> exercises = await _routineRepository.GetRoutineForDayAsync(day);

                if (exercises.Count == 0)
                {
                    continue;
                }

                string exerciseNames = string.Join(", ", exercises.Select(e => e.ExerciseName));

                _ = await LocalNotificationCenter.Current.Show(
                    new NotificationRequest
                    {
                        NotificationId = (int)day + 100,
                        Title = "Today's Exercises",
                        Description = exerciseNames,
                        Schedule = new NotificationRequestSchedule
                        {
#if DEBUG
                            // Fire quickly and repeat every 5 min for easy testing
                            NotifyTime = DateTime.Now.AddSeconds(30),
                            RepeatType = NotificationRepeat.TimeInterval,
                            NotifyRepeatInterval = TimeSpan.FromMinutes(5),
#else
                            NotifyTime = GetNextOccurrence(day, 10, 0),
                            RepeatType = NotificationRepeat.Weekly,
#endif
                        },
                    }
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notifications");
        }
    }

    /// <summary>
    /// Returns true if the app is allowed to schedule exact alarms.
    /// Always true on Android below 12, where the permission does not exist.
    /// </summary>
    private static bool CanScheduleExactAlarms()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            return true;
        }

        return (
                Android.App.Application.Context.GetSystemService(
                    Android.Content.Context.AlarmService
                ) as Android.App.AlarmManager
            )?.CanScheduleExactAlarms() ?? false;
    }

    /// <summary>
    /// Navigates the user to the exact alarm settings screen (Android 12+),
    /// falling back to the app's general settings page if unavailable.
    /// </summary>
    private static void OpenExactAlarmSettings()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            try
            {
                using Android.Content.Intent intent = new(
                    Android.Provider.Settings.ActionRequestScheduleExactAlarm
                );
                _ = intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
                return;
            }
            catch { }
        }

        try
        {
            using Android.Content.Intent fallback = new(
                Android.Provider.Settings.ActionApplicationDetailsSettings
            );
            _ = fallback.SetData(
                Android.Net.Uri.Parse($"package:{Android.App.Application.Context.PackageName}")
            );
            _ = fallback.AddFlags(Android.Content.ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(fallback);
        }
        catch { }
    }

    /// <summary>
    /// Returns the next DateTime when the given day of week occurs at the specified time.
    /// If that slot already passed this week, returns the occurrence next week.
    /// </summary>
    // Used in release builds via the #else branch of the #if DEBUG block above
#pragma warning disable IDE0051
    private static DateTime GetNextOccurrence(DayOfWeek day, int hour, int minute)
    {
        DateTime now = DateTime.Now;
        DateTime next = new(now.Year, now.Month, now.Day, hour, minute, 0);

        int daysUntil = ((int)day - (int)now.DayOfWeek + 7) % 7;

        // If today is the target day but the time has already passed, push to next week
        if (daysUntil == 0 && now.TimeOfDay >= new TimeSpan(hour, minute, 0))
        {
            daysUntil = 7;
        }

        return next.AddDays(daysUntil);
    }
#pragma warning restore IDE0051
}
