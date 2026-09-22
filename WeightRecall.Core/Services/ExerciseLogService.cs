using Microsoft.Extensions.Logging;
using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

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
            // 3. Get the MOST RECENT log before today (regardless of how many days ago)
            ExerciseLog? previousLog = await _repository.GetLatestLogForExerciseAsync(
                planned.ExerciseName,
                selectedDate.Date.AddDays(-1) // Ensures we don't pick up "today" as "previous"
            );

            result.Add(
                DailyExerciseLogBuilder.Build(
                    planned,
                    DailyExerciseLogBuilder.FindExistingLog(existingLogsForDay, planned),
                    previousLog,
                    selectedDate
                )
            );
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
        foreach (ExerciseLog log in logs.Where(l => l.HasRecordedActivity))
        {
            _ = await _repository.SaveExerciseLogAsync(log);
        }
    }
}
