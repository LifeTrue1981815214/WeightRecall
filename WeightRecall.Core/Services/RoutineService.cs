using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing business logic related to workout routines.
/// </summary>
/// <param name="repository">The routine repository.</param>
/// <param name="notificationService">The notification service to sync reminders.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class RoutineService(
    RoutineRepository repository,
    IWorkoutNotificationService notificationService,
    ILogger<RoutineService> logger
)
{
    private readonly RoutineRepository _repository = repository;
    private readonly IWorkoutNotificationService _notificationService = notificationService;
    private readonly ILogger<RoutineService> _logger = logger;

    /// <summary>
    /// Retrieves the list of exercises for a specific day.
    /// </summary>
    /// <param name="day">Day of the week.</param>
    /// <returns>A list of <see cref="RoutineItem"/>.</returns>
    public async Task<List<RoutineItem>> GetRoutineForDay(DayOfWeek day)
    {
        _logger.LogDebug("Retrieving routine for {Day}", day);
        return await _repository.GetRoutineForDayAsync(day);
    }

    /// <summary>
    /// Deletes a routine item and updates the notifications.
    /// </summary>
    /// <param name="item">The item to delete.</param>
    /// <returns>Number of rows affected.</returns>
    public async Task<int> DeleteRoutineItem(RoutineItem item)
    {
        _logger.LogInformation(
            "Deleting routine item: {Exercise} (Id: {Id})",
            item.ExerciseName,
            item.Id
        );
        int result = await _repository.DeleteRoutineItemAsync(item);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Adds a new routine item and updates the notifications.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    public async Task<int> AddRoutineItem(RoutineItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ExerciseName))
        {
            throw new ArgumentException("Exercise name is required.");
        }

        int result = await _repository.AddRoutineItemAsync(item);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Updates an existing routine item and updates notifications.
    /// </summary>
    /// <param name="item">The item to update.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    public async Task<int> UpdateRoutineItem(RoutineItem item)
    {
        if (string.IsNullOrWhiteSpace(item.ExerciseName))
        {
            throw new ArgumentException("Exercise name is required.");
        }

        int result = await _repository.UpdateRoutineItemAsync(item);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Reorders items for a given day sequentially based on their current order and name.
    /// </summary>
    /// <param name="day">Day of the week to reorder.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReorderRoutineItemsAsync(DayOfWeek day)
    {
        List<RoutineItem> items = await _repository.GetRoutineForDayAsync(day);
        List<RoutineItem> sortedItems =
        [
            .. items.OrderBy(i => i.Order).ThenBy(i => i.ExerciseName),
        ];

        for (int i = 0; i < sortedItems.Count; i++)
        {
            int correctOrder = i + 1;
            if (sortedItems[i].Order != correctOrder)
            {
                sortedItems[i].Order = correctOrder;
                _ = await _repository.UpdateRoutineItemAsync(sortedItems[i]);
            }
        }
        await _notificationService.ScheduleDailyNotifications();
    }

    /// <summary>
    /// Saves changes to a routine item and reorders items for relevant days.
    /// </summary>
    /// <param name="item">The routine item to apply changes for.</param>
    /// <param name="oldDay">Optional previous day if the item was moved between days.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ApplyRoutineChangesAsync(RoutineItem item, DayOfWeek? oldDay = null)
    {
        _ = item.Id == 0 ? await AddRoutineItem(item) : await UpdateRoutineItem(item);

        await ReorderRoutineItemsAsync(item.DayOfWeek);

        if (oldDay.HasValue && oldDay.Value != item.DayOfWeek)
        {
            await ReorderRoutineItemsAsync(oldDay.Value);
        }
    }

    /// <summary>
    /// Deletes a routine item and reorders the remaining items for that day.
    /// </summary>
    /// <param name="item">The item to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeleteRoutineItemAndReorderAsync(RoutineItem item)
    {
        _ = await _repository.DeleteRoutineItemAsync(item);
        await ReorderRoutineItemsAsync(item.DayOfWeek);
    }
}
