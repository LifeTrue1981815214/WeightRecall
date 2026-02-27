using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// Repository for managing workout logs in the database.
/// </summary>
/// <param name="context">The database context for data access.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class WorkoutLogRepository(DatabaseContext context, ILogger<WorkoutLogRepository> logger)
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<WorkoutLogRepository> _logger = logger;

    private async Task<SQLite.SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _context.InitializeAsync();
        return _context.Connection;
    }

    /// <summary>
    /// Retrieves all workout logs from the database.
    /// </summary>
    /// <returns>A list of all <see cref="WorkoutLog"/> entries.</returns>
    public async Task<List<WorkoutLog>> GetWorkoutLogsAsync()
    {
        try
        {
            return await (await GetConnectionAsync()).Table<WorkoutLog>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workout logs");
            throw;
        }
    }

    /// <summary>
    /// Retrieves workout logs for a specific date.
    /// </summary>
    /// <param name="date">The date to retrieve logs for.</param>
    /// <returns>A list of <see cref="WorkoutLog"/> entries for the specified date.</returns>
    public async Task<List<WorkoutLog>> GetWorkoutLogForDateAsync(DateTime date)
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<WorkoutLog>()
                .Where(r => r.Date == date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workout logs for {Date}", date);
            throw;
        }
    }

    /// <summary>
    /// Saves a workout log entry to the database (inserts if new, updates if existing).
    /// </summary>
    /// <param name="item">The workout log entry to save.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> SaveWorkoutLogAsync(WorkoutLog item)
    {
        try
        {
            SQLite.SQLiteAsyncConnection connection = await GetConnectionAsync();
            if (item.Id == 0)
            {
                _logger.LogInformation(
                    "Inserting new workout log for {Exercise}",
                    item.ExerciseName
                );
                return await connection.InsertAsync(item);
            }
            else
            {
                _logger.LogInformation(
                    "Updating workout log {Id} for {Exercise}",
                    item.Id,
                    item.ExerciseName
                );
                return await connection.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workout log for {Exercise}", item.ExerciseName);
            throw;
        }
    }

    /// <summary>
    /// Deletes a workout log entry from the database.
    /// </summary>
    /// <param name="item">The workout log entry to delete.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> DeleteWorkoutLogAsync(WorkoutLog item)
    {
        try
        {
            _logger.LogInformation("Deleting workout log {Id}", item.Id);
            return await (await GetConnectionAsync()).DeleteAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workout log {Id}", item.Id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves logs for a specific exercise within a given date range.
    /// </summary>
    /// <param name="exerciseName">The name of the exercise.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <returns>A list of matching <see cref="WorkoutLog"/> entries.</returns>
    public async Task<List<WorkoutLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    )
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<WorkoutLog>()
                .Where(w =>
                    w.ExerciseName == exerciseName && w.Date >= startDate && w.Date <= endDate
                )
                .OrderBy(w => w.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get logs for {Exercise} between {Start} and {End}",
                exerciseName,
                startDate,
                endDate
            );
            throw;
        }
    }
}
