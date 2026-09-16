using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// Repository for managing exercise logs in the database.
/// </summary>
/// <param name="context">The database context for data access.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class ExerciseLogRepository(DatabaseContext context, ILogger<ExerciseLogRepository> logger)
    : IExerciseLogRepository
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<ExerciseLogRepository> _logger = logger;

    private async Task<SQLite.SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _context.InitializeAsync();
        return _context.Connection;
    }

    /// <summary>
    /// Retrieves all exercise logs from the database.
    /// </summary>
    /// <returns>A list of all <see cref="ExerciseLog"/> entries.</returns>
    public async Task<List<ExerciseLog>> GetExerciseLogsAsync()
    {
        try
        {
            return await (await GetConnectionAsync()).Table<ExerciseLog>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get exercise logs");
            throw;
        }
    }

    /// <summary>
    /// Retrieves exercise logs for a specific date.
    /// </summary>
    /// <param name="date">The date to retrieve logs for.</param>
    /// <returns>A list of <see cref="ExerciseLog"/> entries for the specified date.</returns>
    public async Task<List<ExerciseLog>> GetExerciseLogForDateAsync(DateTime date)
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<ExerciseLog>()
                .Where(r => r.Date == date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get exercise logs for {Date}", date);
            throw;
        }
    }

    /// <summary>
    /// Retrieves the most recent exercise log for an exercise on or before the specified date.
    /// </summary>
    /// <param name="exerciseName">The exercise name.</param>
    /// <param name="beforeDate">The latest date to consider (inclusive).</param>
    /// <returns>The latest <see cref="ExerciseLog"/> or null if none found.</returns>
    public async Task<ExerciseLog?> GetLatestLogForExerciseAsync(
        string exerciseName,
        DateTime beforeDate
    )
    {
        try
        {
            List<ExerciseLog> list = await (await GetConnectionAsync())
                .Table<ExerciseLog>()
                .Where(w => w.ExerciseName == exerciseName && w.Date <= beforeDate)
                .OrderByDescending(w => w.Date)
                .Take(1)
                .ToListAsync();

            return list.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get latest exercise log for {Exercise} before {Date}",
                exerciseName,
                beforeDate
            );
            throw;
        }
    }

    /// <summary>
    /// Saves an exercise log entry to the database (inserts if new, updates if existing).
    /// </summary>
    /// <param name="item">The exercise log entry to save.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> SaveExerciseLogAsync(ExerciseLog item)
    {
        try
        {
            SQLite.SQLiteAsyncConnection connection = await GetConnectionAsync();
            if (item.Id == 0)
            {
                _logger.LogInformation(
                    "Inserting new exercise log for {Exercise}",
                    item.ExerciseName
                );
                return await connection.InsertAsync(item);
            }
            else
            {
                _logger.LogInformation(
                    "Updating exercise log {Id} for {Exercise}",
                    item.Id,
                    item.ExerciseName
                );
                return await connection.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save exercise log for {Exercise}", item.ExerciseName);
            throw;
        }
    }

    /// <summary>
    /// Deletes an exercise log entry from the database.
    /// </summary>
    /// <param name="item">The exercise log entry to delete.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> DeleteExerciseLogAsync(ExerciseLog item)
    {
        try
        {
            _logger.LogInformation("Deleting exercise log {Id}", item.Id);
            return await (await GetConnectionAsync()).DeleteAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete exercise log {Id}", item.Id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves logs for a specific exercise within a given date range.
    /// </summary>
    /// <param name="exerciseName">The name of the exercise.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <returns>A list of matching <see cref="ExerciseLog"/> entries.</returns>
    public async Task<List<ExerciseLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    )
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<ExerciseLog>()
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

    /// <inheritdoc />
    public async Task<int> RenameExerciseAsync(string previousName, string newName)
    {
        try
        {
            SQLite.SQLiteAsyncConnection connection = await GetConnectionAsync();
            int moved = await connection.ExecuteAsync(
                "UPDATE " + ExerciseLog.TableName + " SET ExerciseName = ? WHERE ExerciseName = ?",
                newName,
                previousName
            );

            _logger.LogInformation(
                "Moved {Count} exercise log(s) from {Previous} to {New}",
                moved,
                previousName,
                newName
            );

            return moved;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to move exercise logs from {Previous} to {New}",
                previousName,
                newName
            );
            throw;
        }
    }
}
