using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

public class RoutineRepository(DatabaseContext context, ILogger<RoutineRepository> logger)
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<RoutineRepository> _logger = logger;

    private async Task<SQLite.SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _context.InitializeAsync();
        return _context.Connection;
    }

    public async Task<List<RoutineItem>> GetRoutineItemsAsync()
    {
        try
        {
            return await (await GetConnectionAsync()).Table<RoutineItem>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all routine items");
            throw;
        }
    }

    public async Task<List<RoutineItem>> GetRoutineForDayAsync(DayOfWeek day)
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<RoutineItem>()
                .Where(r => r.DayOfWeek == day)
                .OrderBy(r => r.Order)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get routine for {Day}", day);
            throw;
        }
    }

    public async Task<int> AddRoutineItemAsync(RoutineItem item)
    {
        try
        {
            _logger.LogInformation("Adding routine item: {Name}", item.ExerciseName);
            return await (await GetConnectionAsync()).InsertAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add routine item {Name}", item.ExerciseName);
            throw;
        }
    }

    public async Task<int> DeleteRoutineItemAsync(RoutineItem item)
    {
        try
        {
            _logger.LogInformation("Deleting routine item: {Id}", item.Id);
            return await (await GetConnectionAsync()).DeleteAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete routine item {Id}", item.Id);
            throw;
        }
    }

    public async Task<int> UpdateRoutineItemAsync(RoutineItem item)
    {
        try
        {
            _logger.LogInformation("Updating routine item: {Id}", item.Id);
            return await (await GetConnectionAsync()).UpdateAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update routine item {Id}", item.Id);
            throw;
        }
    }
}
