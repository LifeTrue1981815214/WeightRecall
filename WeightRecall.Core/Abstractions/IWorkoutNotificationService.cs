namespace WeightRecall.Abstractions;

/// <summary>
/// Schedules the reminders for the days that have exercises planned.
/// </summary>
public interface IWorkoutNotificationService
{
    Task ScheduleDailyNotifications();
}
