using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

namespace WeightRecall.Services;

/// <summary>
/// Service for turning logged exercise history into progress data points.
/// </summary>
/// <remarks>
/// Split out from <see cref="ExerciseLogService"/>: recording a workout and charting its history
/// are separate concerns with separate callers, and this half needs neither the planned exercises
/// nor anything else recording a workout depends on.
/// </remarks>
/// <param name="repository">The exercise log repository.</param>
/// <param name="timeProvider">
/// Supplies the current date. Injected rather than read from <see cref="DateTime.Today"/> so tests
/// can pin "today" and cover the month-boundary behaviour of the reporting window.
/// </param>
public class ExerciseProgressService(IExerciseLogRepository repository, TimeProvider timeProvider)
{
    private readonly IExerciseLogRepository _repository = repository;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Retrieves exercise logs for a specific exercise over the last month.
    /// </summary>
    /// <remarks>
    /// The window is one calendar month back, not a fixed 30 days, so its length varies with the
    /// month. The Progress page currently labels it "Last 30 Days", which does not always match.
    /// </remarks>
    /// <param name="exerciseName">The exercise to track.</param>
    /// <returns>A list of <see cref="ExerciseLog"/> entries within the window.</returns>
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
    /// <param name="exerciseName">The name of the exercise.</param>
    /// <returns>A list of <see cref="ExerciseProgressPoint"/> data points.</returns>
    public async Task<List<ExerciseProgressPoint>> GetExerciseProgressHistoryAsync(
        string exerciseName
    )
    {
        List<ExerciseLog> logs = await GetExerciseProgressLastMonth(exerciseName);
        return ExerciseProgressHistory.Summarize(logs);
    }
}
