using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing exercise logs and computing exercise progress.
/// </summary>
/// <param name="repository">The exercise log repository.</param>
/// <param name="plannedExerciseRepository">The planned exercise repository to cross-reference exercises.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class ExerciseLogService(
    IExerciseLogRepository repository,
    IPlannedExerciseRepository plannedExerciseRepository,
    ILogger<ExerciseLogService> logger
)
{
    private readonly IExerciseLogRepository _repository = repository;
    private readonly IPlannedExerciseRepository _plannedExerciseRepository =
        plannedExerciseRepository;
    private readonly ILogger<ExerciseLogService> _logger = logger;

    /// <summary>
    /// Retrieves all exercise logs for a given date.
    /// </summary>
    /// <param name="date">Target date.</param>
    /// <returns>A list of <see cref="ExerciseLog"/> entries.</returns>
    public async Task<List<ExerciseLog>> GetExerciseLogsForDate(DateTime date)
    {
        _logger.LogDebug("Retrieving exercise logs for {Date}", date);
        return await _repository.GetExerciseLogForDateAsync(date);
    }

    /// <summary>
    /// Saves a single exercise log entry.
    /// </summary>
    /// <param name="log">The exercise log to save.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> SaveExerciseLog(ExerciseLog log)
    {
        _logger.LogInformation("Saving exercise log for {Exercise}", log.ExerciseName);
        return await _repository.SaveExerciseLogAsync(log);
    }

    /// <summary>
    /// Deletes an exercise log entry.
    /// </summary>
    /// <param name="log">The exercise log to delete.</param>
    /// <returns>The number of rows affected.</returns>
    public async Task<int> DeleteExerciseLog(ExerciseLog log)
    {
        _logger.LogInformation("Deleting exercise log: {Id}", log.Id);
        return await _repository.DeleteExerciseLogAsync(log);
    }

    /// <summary>
    /// Gets the list of exercise logs for a selected date, pre-populated with exercises from the routine for that day.
    /// Also fetches historical data from the previous week to provide context.
    /// </summary>
    /// <param name="selectedDate">The date chosen by the user.</param>
    /// <returns>A list of exercise logs representing the daily plan and any existing data.</returns>
    public async Task<List<ExerciseLog>> GetDailyExerciseLogsAsync(DateTime selectedDate)
    {
        // 1. Get the routine definition for this day of the week
        List<PlannedExercise> routine =
            await _plannedExerciseRepository.GetPlannedExercisesForDayAsync(selectedDate.DayOfWeek);

        // 2. Get any existing logs already saved for this specific date
        List<ExerciseLog> existingLogsForDay = await _repository.GetExerciseLogForDateAsync(
            selectedDate.Date
        );

        List<ExerciseLog> result = [];

        foreach (PlannedExercise planned in routine)
        {
            // Check if the user already started/saved this exercise today
            ExerciseLog? existingLog = existingLogsForDay.FirstOrDefault(l =>
                l.ExerciseName.Equals(planned.ExerciseName, StringComparison.OrdinalIgnoreCase)
            );

            // 3. Get the MOST RECENT log before today (regardless of how many days ago)
            ExerciseLog? prevLog = await _repository.GetLatestLogForExerciseAsync(
                planned.ExerciseName,
                selectedDate.Date.AddDays(-1) // Ensures we don't pick up "today" as "previous"
            );

            string prevDesc =
                prevLog != null
                    ? $"Prev: {prevLog.Weight}kg | {prevLog.Sets} sets | {prevLog.Reps} reps"
                    : "No data from last week";

            if (existingLog != null)
            {
                // If it exists, just update the description for the UI
                existingLog.PreviousDescription = prevDesc;
                result.Add(existingLog);
            }
            else
            {
                // If it's a new entry for the day, create the placeholder
                result.Add(
                    new ExerciseLog
                    {
                        Date = selectedDate.Date,
                        ExerciseName = planned.ExerciseName,
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
    /// Saves multiple exercise log entries, skipping those with no recorded activity.
    /// </summary>
    /// <param name="logs">Collection of exercise logs to save.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveExerciseLogsAsync(IEnumerable<ExerciseLog> logs)
    {
        foreach (ExerciseLog exercise in logs)
        {
            if (exercise.Weight > 0 || exercise.Sets > 0 || exercise.Reps > 0)
            {
                _ = await _repository.SaveExerciseLogAsync(exercise);
            }
        }
    }

    /// <summary>
    /// Retrieves exercise logs for a specific exercise over the last month.
    /// </summary>
    /// <param name="exerciseName">The exercise to track.</param>
    /// <returns>A list of <see cref="ExerciseLog"/> entries from the last 30 days.</returns>
    public async Task<List<ExerciseLog>> GetExerciseProgressLastMonth(string exerciseName)
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
        List<ExerciseLog> logs = await GetExerciseProgressLastMonth(exerciseName);

        return
        [
            .. logs.GroupBy(l => l.Date.Date)
                .Select(g => new ExerciseProgressPoint(g.Key, g.Max(l => l.Weight)))
                .OrderBy(p => p.Date),
        ];
    }
}
