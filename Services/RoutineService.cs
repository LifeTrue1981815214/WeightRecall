using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

public class RoutineService(
    RoutineRepository repository,
    NotificationService notificationService,
    ILogger<RoutineService> logger
)
{
    private readonly RoutineRepository _repository = repository;
    private readonly NotificationService _notificationService = notificationService;
    private readonly ILogger<RoutineService> _logger = logger;

    public async Task<List<RoutineItem>> GetRoutineForDay(DayOfWeek day)
    {
        _logger.LogDebug("Retrieving routine for {Day}", day);
        return await _repository.GetRoutineForDayAsync(day);
    }

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

    public async Task ApplyRoutineChangesAsync(RoutineItem item, DayOfWeek? oldDay = null)
    {
        _ = item.Id == 0 ? await AddRoutineItem(item) : await UpdateRoutineItem(item);

        await ReorderRoutineItemsAsync(item.DayOfWeek);

        if (oldDay.HasValue && oldDay.Value != item.DayOfWeek)
        {
            await ReorderRoutineItemsAsync(oldDay.Value);
        }
    }

    public async Task DeleteRoutineItemAndReorderAsync(RoutineItem item)
    {
        _ = await _repository.DeleteRoutineItemAsync(item);
        await ReorderRoutineItemsAsync(item.DayOfWeek);
    }
}
