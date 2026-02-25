using Plugin.LocalNotification;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

public class NotificationService(RoutineRepository repository)
{
    private readonly RoutineRepository _repository = repository;

    public async Task<bool> RequestNotificationPermission()
    {
        return await LocalNotificationCenter.Current.RequestNotificationPermission();
    }

    public async Task ScheduleDailyNotifications()
    {
        // First cancel all previous notifications to avoid duplicates
        _ = LocalNotificationCenter.Current.CancelAll();

        // Check user preference
        bool notificationsEnabled = Preferences.Default.Get("NotificationsEnabled", true);
        if (!notificationsEnabled)
        {
            return;
        }

        // Check if permission is granted
        if (!await LocalNotificationCenter.Current.AreNotificationsEnabled())
        {
            return;
        }

        // We want to schedule a notification for each day of the week if there are exercises
        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            List<RoutineItem> exercises = await _repository.GetRoutineForDayAsync(day);

            if (exercises.Count > 0)
            {
                string exerciseList = string.Join(", ", exercises.Select(e => e.ExerciseName));

                NotificationRequest notification = new()
                {
                    NotificationId = (int)day + 100, // Unique ID for each day
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
    }

    private static DateTime GetNextOccurrence(DayOfWeek day, int hour, int minute)
    {
        DateTime now = DateTime.Now;
        DateTime next = new(now.Year, now.Month, now.Day, hour, minute, 0);

        // If it's already past 10 AM today, or it's not today, move to the target day
        int daysUntil = ((int)day - (int)now.DayOfWeek + 7) % 7;

        if (daysUntil == 0 && now.TimeOfDay >= new TimeSpan(hour, minute, 0))
        {
            daysUntil = 7;
        }

        return next.AddDays(daysUntil);
    }
}
