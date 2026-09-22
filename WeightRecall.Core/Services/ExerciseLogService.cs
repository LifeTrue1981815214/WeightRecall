using Microsoft.Extensions.Logging;
using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing exercise logs and computing exercise progress.
/// </summary>
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
    public async Task<List<ExerciseLog>> GetExerciseLogsForDate(DateTime date)
    {
        _logger.LogDebug("Retrieving exercise logs for {Date}", date);
        return await _repository.GetExerciseLogForDateAsync(date);
    }

    /// <summary>
    /// Saves a single exercise log entry.
    /// </summary>
    public async Task<int> SaveExerciseLog(ExerciseLog log)
    {
        _logger.LogInformation("Saving exercise log for {Exercise}", log.ExerciseName);
        return await _repository.SaveExerciseLogAsync(log);
    }

    /// <summary>
    /// Deletes an exercise log entry.
    /// </summary>
    public async Task<int> DeleteExerciseLog(ExerciseLog log)
    {
        _logger.LogInformation("Deleting exercise log: {Id}", log.Id);
        return await _repository.DeleteExerciseLogAsync(log);
    }

    /// <summary>
    /// Builds the day's sheet: one row per planned exercise, carrying anything already logged
    /// for that date and a description of the last time each was done.
    /// </summary>
    public async Task<List<ExerciseLog>> GetDailyExerciseLogsAsync(DateTime selectedDate)
    {
        List<PlannedExercise> routine =
            await _plannedExerciseRepository.GetPlannedExercisesForDayAsync(selectedDate.DayOfWeek);

        List<ExerciseLog> existingLogsForDay = await _repository.GetExerciseLogForDateAsync(
            selectedDate.Date
        );

        List<ExerciseLog> result = [];

        foreach (PlannedExercise planned in routine)
        {
            // The most recent log before this date, however long ago it was.
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
    public async Task SaveExerciseLogsAsync(IEnumerable<ExerciseLog> logs)
    {
        foreach (ExerciseLog log in logs.Where(l => l.HasRecordedActivity))
        {
            _ = await _repository.SaveExerciseLogAsync(log);
        }
    }
}
