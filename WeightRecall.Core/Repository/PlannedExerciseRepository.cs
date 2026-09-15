using Microsoft.Extensions.Logging;
using SQLite;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// SQLite-backed implementation of <see cref="IPlannedExerciseRepository"/>.
/// </summary>
/// <param name="context">The database context for data access.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class PlannedExerciseRepository(
    DatabaseContext context,
    ILogger<PlannedExerciseRepository> logger
) : IPlannedExerciseRepository
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<PlannedExerciseRepository> _logger = logger;

    /// <inheritdoc />
    public Task<List<PlannedExercise>> GetPlannedExercisesAsync()
    {
        return ExecuteAsync(
            connection => connection.Table<PlannedExercise>().ToListAsync(),
            ex => _logger.LogError(ex, "Failed to get all planned exercises")
        );
    }

    /// <inheritdoc />
    public Task<PlannedExercise?> GetPlannedExerciseAsync(int id)
    {
        return ExecuteAsync<PlannedExercise?>(
            async connection => await connection.FindAsync<PlannedExercise>(id),
            ex => _logger.LogError(ex, "Failed to get planned exercise {Id}", id)
        );
    }

    /// <inheritdoc />
    public Task<List<PlannedExercise>> GetPlannedExercisesForDayAsync(DayOfWeek day)
    {
        return ExecuteAsync(
            connection =>
                connection
                    .Table<PlannedExercise>()
                    .Where(e => e.DayOfWeek == day)
                    .OrderBy(e => e.Order)
                    .ToListAsync(),
            ex => _logger.LogError(ex, "Failed to get planned exercises for {Day}", day)
        );
    }

    /// <inheritdoc />
    public Task<int> AddPlannedExerciseAsync(PlannedExercise exercise)
    {
        _logger.LogInformation("Adding planned exercise: {Name}", exercise.ExerciseName);
        return ExecuteAsync(
            connection => connection.InsertAsync(exercise),
            ex =>
                _logger.LogError(ex, "Failed to add planned exercise {Name}", exercise.ExerciseName)
        );
    }

    /// <inheritdoc />
    public Task<int> DeletePlannedExerciseAsync(PlannedExercise exercise)
    {
        _logger.LogInformation("Deleting planned exercise: {Id}", exercise.Id);
        return ExecuteAsync(
            connection => connection.DeleteAsync(exercise),
            ex => _logger.LogError(ex, "Failed to delete planned exercise {Id}", exercise.Id)
        );
    }

    /// <inheritdoc />
    public Task<int> UpdatePlannedExerciseAsync(PlannedExercise exercise)
    {
        _logger.LogInformation("Updating planned exercise: {Id}", exercise.Id);
        return ExecuteAsync(
            connection => connection.UpdateAsync(exercise),
            ex => _logger.LogError(ex, "Failed to update planned exercise {Id}", exercise.Id)
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
