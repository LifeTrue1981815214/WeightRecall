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
    /// Retrieves exercise logs for a specific exercise over the last month.
    /// </summary>
    /// <remarks>
    /// One calendar month back, not a fixed 30 days, so its length varies. The Progress page
    /// labels it "Last 30 Days", which does not always match.
    /// </remarks>
    public async Task<List<ExerciseLog>> GetExerciseProgressLastMonth(string exerciseName)
    {
        DateTime endDate = _timeProvider.GetLocalNow().Date;
        DateTime startDate = endDate.AddMonths(-1);
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
