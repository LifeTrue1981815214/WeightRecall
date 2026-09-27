using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

namespace WeightRecall.Services;

/// <summary>
/// Service for turning logged exercise history into progress data points.
/// </summary>
/// <param name="timeProvider">
/// Injected rather than read from <see cref="DateTime.Today"/> so tests can pin "today".
/// </param>
public class ExerciseProgressService(IExerciseLogRepository repository, TimeProvider timeProvider)
{
    private readonly IExerciseLogRepository _repository = repository;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Retrieves exercise logs for a specific exercise over the last 30 days.
    /// </summary>
    public async Task<List<ExerciseLog>> GetExerciseProgressLastMonth(string exerciseName)
    {
        DateTime endDate = _timeProvider.GetLocalNow().Date;
        DateTime startDate = endDate.AddDays(-30);
        return await _repository.GetLogsForExerciseInDateRangeAsync(
            exerciseName,
            startDate,
            endDate
        );
    }

    /// <summary>
    /// Aggregates workout history for an exercise into a simplified progress history.
    /// </summary>
    public async Task<List<ExerciseProgressPoint>> GetExerciseProgressHistoryAsync(
        string exerciseName
    )
    {
        List<ExerciseLog> logs = await GetExerciseProgressLastMonth(exerciseName);
        return ExerciseProgressHistory.Summarize(logs);
    }
}
