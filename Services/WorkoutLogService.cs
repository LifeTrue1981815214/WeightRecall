using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

public class WorkoutLogService(
    WorkoutLogRepository repository,
    RoutineRepository routineRepository,
    ILogger<WorkoutLogService> logger
)
{
    private readonly WorkoutLogRepository _repository = repository;
    private readonly RoutineRepository _routineRepository = routineRepository;
    private readonly ILogger<WorkoutLogService> _logger = logger;

    public async Task<List<WorkoutLog>> GetWorkoutLogForExercise(DateTime date)
    {
        _logger.LogDebug("Retrieving workout logs for {Date}", date);
        return await _repository.GetWorkoutLogForDateAsync(date);
    }

    public async Task<int> SaveWorkoutLog(WorkoutLog workout)
    {
        _logger.LogInformation("Saving workout log for {Exercise}", workout.ExerciseName);
        return await _repository.SaveWorkoutLogAsync(workout);
    }

    public async Task<int> DeleteWorkoutLog(WorkoutLog workout)
    {
        _logger.LogInformation("Deleting workout log: {Id}", workout.Id);
        return await _repository.DeleteWorkoutLogAsync(workout);
    }

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
