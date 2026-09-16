using Microsoft.Extensions.Logging;
using SQLite;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// SQLite-backed implementation of <see cref="IExerciseLogRepository"/>.
/// </summary>
/// <param name="context">The database context for data access.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class ExerciseLogRepository(DatabaseContext context, ILogger<ExerciseLogRepository> logger)
    : IExerciseLogRepository
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<ExerciseLogRepository> _logger = logger;

    /// <inheritdoc />
    public Task<List<ExerciseLog>> GetExerciseLogsAsync()
    {
        return ExecuteAsync(
            connection => connection.Table<ExerciseLog>().ToListAsync(),
            ex => _logger.LogError(ex, "Failed to get exercise logs")
        );
    }

    /// <inheritdoc />
    public Task<List<ExerciseLog>> GetExerciseLogForDateAsync(DateTime date)
    {
        return ExecuteAsync(
            connection => connection.Table<ExerciseLog>().Where(r => r.Date == date).ToListAsync(),
            ex => _logger.LogError(ex, "Failed to get exercise logs for {Date}", date)
        );
    }

    /// <inheritdoc />
    public Task<ExerciseLog?> GetLatestLogForExerciseAsync(string exerciseName, DateTime beforeDate)
    {
        return ExecuteAsync<ExerciseLog?>(
            async connection =>
            {
                List<ExerciseLog> list = await connection
                    .Table<ExerciseLog>()
                    .Where(w => w.ExerciseName == exerciseName && w.Date <= beforeDate)
                    .OrderByDescending(w => w.Date)
                    .Take(1)
                    .ToListAsync();

                return list.FirstOrDefault();
            },
            ex =>
                _logger.LogError(
                    ex,
                    "Failed to get latest exercise log for {Exercise} before {Date}",
                    exerciseName,
                    beforeDate
                )
        );
    }

    /// <inheritdoc />
    public Task<int> SaveExerciseLogAsync(ExerciseLog item)
    {
        return ExecuteAsync(
            connection =>
            {
                if (item.Id == 0)
                {
                    _logger.LogInformation(
                        "Inserting new exercise log for {Exercise}",
                        item.ExerciseName
                    );
                    return connection.InsertAsync(item);
                }

                _logger.LogInformation(
                    "Updating exercise log {Id} for {Exercise}",
                    item.Id,
                    item.ExerciseName
                );
                return connection.UpdateAsync(item);
            },
            ex =>
                _logger.LogError(
                    ex,
                    "Failed to save exercise log for {Exercise}",
                    item.ExerciseName
                )
        );
    }

    /// <inheritdoc />
    public Task<int> DeleteExerciseLogAsync(ExerciseLog item)
    {
        _logger.LogInformation("Deleting exercise log {Id}", item.Id);
        return ExecuteAsync(
            connection => connection.DeleteAsync(item),
            ex => _logger.LogError(ex, "Failed to delete exercise log {Id}", item.Id)
        );
    }

    /// <inheritdoc />
    public Task<List<ExerciseLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    )
    {
        return ExecuteAsync(
            connection =>
                connection
                    .Table<ExerciseLog>()
                    .Where(w =>
                        w.ExerciseName == exerciseName && w.Date >= startDate && w.Date <= endDate
                    )
                    .OrderBy(w => w.Date)
                    .ToListAsync(),
            ex =>
                _logger.LogError(
                    ex,
                    "Failed to get logs for {Exercise} between {Start} and {End}",
                    exerciseName,
                    startDate,
                    endDate
                )
        );
    }

    /// <inheritdoc />
    public Task<int> RenameExerciseAsync(string previousName, string newName)
    {
        return ExecuteAsync(
            async connection =>
            {
                int moved = await connection.ExecuteAsync(
                    "UPDATE "
                        + ExerciseLog.TableName
                        + " SET ExerciseName = ? WHERE ExerciseName = ?",
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
            },
            ex =>
                _logger.LogError(
                    ex,
                    "Failed to move exercise logs from {Previous} to {New}",
                    previousName,
                    newName
                )
        );
    }

    /// <summary>
    /// Runs a database operation against an initialized connection. Any failure is
    /// reported through <paramref name="logFailure"/> and then rethrown unchanged.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="operation">The operation to run against the connection.</param>
    /// <param name="logFailure">Callback that logs the failure before it is rethrown.</param>
    /// <returns>The result of the operation.</returns>
    private async Task<T> ExecuteAsync<T>(
        Func<SQLiteAsyncConnection, Task<T>> operation,
        Action<Exception> logFailure
    )
    {
        try
        {
            await _context.InitializeAsync();
            return await operation(_context.Connection);
        }
        catch (Exception ex)
        {
            logFailure(ex);
            throw;
        }
    }
}
