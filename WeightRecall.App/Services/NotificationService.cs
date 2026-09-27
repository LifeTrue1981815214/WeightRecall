using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using WeightRecall.Abstractions;
// Debug reports this as unnecessary: its only use is in the release branch of the #if below.
using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

namespace WeightRecall.Services;

public class NotificationService(
    IPlannedExerciseRepository plannedExerciseRepository,
    ILogger<NotificationService> logger
) : IWorkoutNotificationService
{
    private readonly IPlannedExerciseRepository _plannedExerciseRepository =
        plannedExerciseRepository;
    private readonly ILogger<NotificationService> _logger = logger;

    /// <summary>
    /// Preferences key holding whether the user wants reminders at all.
    /// </summary>
    public const string NotificationsEnabledKey = "NotificationsEnabled";

    /// <summary>
    /// Preferences key recording that the one-time reminder setup has already run.
    /// </summary>
    public const string RemindersRequestedKey = "RemindersRequested";

    /// <summary>
    /// Asks for POST_NOTIFICATIONS if it has not been granted yet.
    /// </summary>
    /// <remarks>
    /// Shows at most a system dialog, and never sends the user to another screen, so it is safe
    /// to call on startup.
    /// </remarks>
    public static Task<bool> RequestNotificationPermission()
    {
        return LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    /// <summary>
    /// Asks for everything a reminder needs, sending the user to the system's exact-alarm screen
    /// if that permission is missing.
    /// </summary>
    /// <remarks>
    /// Call this only where the user has asked for reminders, or once on first run. It can
    /// navigate out of the app, so calling it on every launch traps anyone who has not granted
    /// exact alarms: they are thrown into system settings each time, with no way to reach the
    /// app. The exact-alarm screen is only opened once notifications have been allowed, so
    /// declining the first prompt ends the flow there.
    /// </remarks>
    /// <returns><c>true</c> when reminders can actually be scheduled.</returns>
    public static async Task<bool> RequestReminderPermissions()
    {
        if (!await RequestNotificationPermission())
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

            if (!Preferences.Default.Get(NotificationsEnabledKey, true))
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
                List<PlannedExercise> exercises =
                    await _plannedExerciseRepository.GetPlannedExercisesForDayAsync(day);

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
                            NotifyTime = WeeklyNotificationSchedule.GetNextOccurrence(
                                day,
                                10,
                                0,
                                DateTime.Now
                            ),
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
}
