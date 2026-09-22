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
