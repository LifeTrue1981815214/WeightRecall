using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing local notifications for exercise reminders.
/// </summary>
/// <param name="repository">The routine repository to fetch scheduled exercises.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class NotificationService(RoutineRepository repository, ILogger<NotificationService> logger)
{
    private readonly RoutineRepository _repository = repository;
    private readonly ILogger<NotificationService> _logger = logger;

    /// <summary>
    /// Requests the user's permission to display local notifications.
    /// </summary>
    /// <returns>True if permission is granted, false otherwise.</returns>
    public async Task<bool> RequestNotificationPermission()
    {
        _logger.LogInformation("Requesting notification permission");
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    /// <summary>
    /// Schedules weekly notifications for each day that has exercises in the routine.
    /// Notifications are cancelled and rescheduled to match current routine state.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ScheduleDailyNotifications()
    {
        try
        {
            _logger.LogInformation("Scheduling daily notifications...");
            _ = LocalNotificationCenter.Current.CancelAll();

            bool notificationsEnabled = Preferences.Default.Get("NotificationsEnabled", true);
            if (!notificationsEnabled)
            {
                _logger.LogInformation("Notifications are disabled in preferences");
                return;
            }

            if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
            {
                _logger.LogWarning("Notifications are not enabled in system settings");
                return;
            }

            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                List<RoutineItem> exercises = await _repository.GetRoutineForDayAsync(day);

                if (exercises.Count > 0)
                {
                    string exerciseList = string.Join(", ", exercises.Select(e => e.ExerciseName));

                    NotificationRequest notification = new()
                    {
                        NotificationId = (int)day + 100,
                        Title = "Today's Exercises",
                        Description = $"{exerciseList}",
                        Schedule = new NotificationRequestSchedule
                        {
                            NotifyTime = GetNextOccurrence(day, 10, 0),
                            RepeatType = NotificationRepeat.Weekly,
                        },
                    };

                    _ = await LocalNotificationCenter.Current.Show(notification);
                }
            }
            _logger.LogInformation("Successfully updated daily notifications");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notifications");
        }
    }

    /// <summary>
    /// Calculates the next occurrence of a specific time on a given day of the week.
    /// </summary>
    /// <param name="day">Target day of the week.</param>
    /// <param name="hour">Target hour (24-hour format).</param>
    /// <param name="minute">Target minute.</param>
    /// <returns>The <see cref="DateTime"/> for the next occurrence.</returns>
    private static DateTime GetNextOccurrence(DayOfWeek day, int hour, int minute)
    {
        DateTime now = DateTime.Now;
        DateTime next = new(now.Year, now.Month, now.Day, hour, minute, 0);

        int daysUntil = ((int)day - (int)now.DayOfWeek + 7) % 7;

        if (daysUntil == 0 && now.TimeOfDay >= new TimeSpan(hour, minute, 0))
        {
            daysUntil = 7;
        }

        return next.AddDays(daysUntil);
    }
}
