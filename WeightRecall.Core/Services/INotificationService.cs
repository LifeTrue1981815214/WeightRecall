namespace WeightRecall.Services;

/// <summary>
/// Abstraction for scheduling workout reminder notifications.
/// </summary>
public interface IWorkoutNotificationService
{
    Task ScheduleDailyNotifications();
}
