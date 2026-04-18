using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// Repository for managing workout routine items in the database.
/// </summary>
/// <param name="context">The database context for data access.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class RoutineRepository(DatabaseContext context, ILogger<RoutineRepository> logger)
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<RoutineRepository> _logger = logger;

    private async Task<SQLite.SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _context.InitializeAsync();
        return _context.Connection;
    }

    /// <summary>
    /// Retrieves all routine items from the database.
    /// </summary>
    /// <returns>A list of all <see cref="RoutineItem"/> entries.</returns>
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

    /// <summary>
    /// Retrieves routine items for a specific day of the week, ordered by their display order.
    /// </summary>
    /// <param name="day">The day of the week to retrieve the routine for.</param>
    /// <returns>A list of <see cref="RoutineItem"/> for the specified day.</returns>
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

    /// <summary>
    /// Adds a new routine item to the database.
    /// </summary>
    /// <param name="item">The routine item to add.</param>
    /// <returns>The number of rows affected.</returns>
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

    /// <summary>
    /// Deletes a routine item from the database.
    /// </summary>
    /// <param name="item">The routine item to delete.</param>
    /// <returns>The number of rows affected.</returns>
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

    /// <summary>
    /// Updates an existing routine item in the database.
    /// </summary>
    /// <param name="item">The routine item to update.</param>
    /// <returns>The number of rows affected.</returns>
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
