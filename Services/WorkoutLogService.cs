using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing workout logs and computing exercise progress.
/// </summary>
/// <param name="repository">The workout log repository.</param>
/// <param name="routineRepository">The routine repository to cross-reference exercises.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class WorkoutLogService(
    WorkoutLogRepository repository,
    RoutineRepository routineRepository,
    ILogger<WorkoutLogService> logger
)
{
    private readonly WorkoutLogRepository _repository = repository;
    private readonly RoutineRepository _routineRepository = routineRepository;
    private readonly ILogger<WorkoutLogService> _logger = logger;

    /// <summary>
    /// Retrieves all workout logs for a given date.
    /// </summary>
    /// <param name="date">Target date.</param>
    /// <returns>A list of <see cref="WorkoutLog"/> entries.</returns>
    public async Task<List<WorkoutLog>> GetWorkoutLogForExercise(DateTime date)
    {
        _logger.LogDebug("Retrieving workout logs for {Date}", date);
        return await _repository.GetWorkoutLogForDateAsync(date);
    }

    /// <summary>
    /// Saves a single workout log entry.
    /// </summary>
    /// <param name="workout">The workout log to save.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> SaveWorkoutLog(WorkoutLog workout)
    {
        _logger.LogInformation("Saving workout log for {Exercise}", workout.ExerciseName);
        return await _repository.SaveWorkoutLogAsync(workout);
    }

    /// <summary>
    /// Deletes a workout log entry.
    /// </summary>
    /// <param name="workout">The workout log to delete.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> DeleteWorkoutLog(WorkoutLog workout)
    {
        _logger.LogInformation("Deleting workout log: {Id}", workout.Id);
        return await _repository.DeleteWorkoutLogAsync(workout);
    }

    /// <summary>
    /// Gets the list of workout logs for a selected date, pre-populated with exercises from the routine for that day.
    /// Also fetches historical data from the previous week to provide context.
    /// </summary>
    /// <param name="selectedDate">The date chosen by the user.</param>
    /// <returns>A list of workout logs representing the daily plan and any existing data.</returns>
    public async Task<List<WorkoutLog>> GetDailyWorkoutLogsAsync(DateTime selectedDate)
    {
        List<RoutineItem> routine = await _routineRepository.GetRoutineForDayAsync(
            selectedDate.DayOfWeek
        );

        List<WorkoutLog> logs = await _repository.GetWorkoutLogForDateAsync(selectedDate.Date);

        List<WorkoutLog> previousLogs = await _repository.GetWorkoutLogForDateAsync(
            selectedDate.Date.AddDays(-7)
        );

        List<WorkoutLog> result = [];

        foreach (RoutineItem item in routine)
        {
            WorkoutLog? existingLog = logs.FirstOrDefault(l =>
                l.ExerciseName.Equals(item.ExerciseName, StringComparison.OrdinalIgnoreCase)
            );

            WorkoutLog? prevLog = previousLogs.FirstOrDefault(l =>
                l.ExerciseName.Equals(item.ExerciseName, StringComparison.OrdinalIgnoreCase)
            );

            string prevDesc =
                prevLog != null
                    ? $"Prev: {prevLog.Weight}kg | {prevLog.Sets} sets | {prevLog.Reps} reps"
                    : "No data from last week";

            if (existingLog != null)
            {
                existingLog.PreviousDescription = prevDesc;
                result.Add(existingLog);
            }
            else
            {
                result.Add(
                    new WorkoutLog
                    {
                        Date = selectedDate.Date,
                        ExerciseName = item.ExerciseName,
                        Weight = 0,
                        Sets = 0,
                        Reps = 0,
                        PreviousDescription = prevDesc,
                    }
                );
            }
        }

        return result;
    }

    /// <summary>
    /// Saves multiple workout log entries, skipping those with no recorded activity.
    /// </summary>
    /// <param name="logs">Collection of workout logs to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveWorkoutLogsAsync(IEnumerable<WorkoutLog> logs)
    {
        foreach (WorkoutLog exercise in logs)
        {
            if (exercise.Weight > 0 || exercise.Sets > 0 || exercise.Reps > 0)
            {
                _ = await _repository.SaveWorkoutLogAsync(exercise);
            }
        }
    }

    /// <summary>
    /// Retrieves workout logs for a specific exercise over the last month.
    /// </summary>
    /// <param name="exerciseName">The exercise to track.</param>
    /// <returns>A list of <see cref="WorkoutLog"/> entries from the last 30 days.</returns>
    public async Task<List<WorkoutLog>> GetExerciseProgressLastMonth(string exerciseName)
    {
        DateTime endDate = DateTime.Today;
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
        List<WorkoutLog> logs = await GetExerciseProgressLastMonth(exerciseName);

        return
        [
            .. logs.GroupBy(l => l.Date.Date)
                .Select(g => new ExerciseProgressPoint(g.Key, g.Max(l => l.Weight)))
                .OrderBy(p => p.Date),
        ];
    }
}
