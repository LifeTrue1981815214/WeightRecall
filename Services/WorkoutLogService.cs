using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

public class WorkoutLogService(WorkoutLogRepository repository, RoutineRepository routineRepository)
{
    private readonly WorkoutLogRepository _repository = repository;
    private readonly RoutineRepository _routineRepository = routineRepository;

    public async Task<List<WorkoutLog>> GetWorkoutLogForExercise(DateTime date)
    {
        return await _repository.GetWorkoutLogForDateAsync(date);
    }

    public async Task<int> SaveWorkoutLog(WorkoutLog workout)
    {
        return await _repository.SaveWorkoutLogAsync(workout);
    }

    public async Task<int> DeleteWorkoutLog(WorkoutLog workout)
    {
        return await _repository.DeleteWorkoutLogAsync(workout);
    }

    public async Task<List<WorkoutLog>> GetDailyWorkoutLogsAsync(DateTime selectedDate)
    {
        // 1. Get the routine for the selected day of the week
        List<RoutineItem> routine = await _routineRepository.GetRoutineForDayAsync(
            selectedDate.DayOfWeek
        );

        // 2. Get existing logs for the selected date
        List<WorkoutLog> logs = await _repository.GetWorkoutLogForDateAsync(selectedDate.Date);

        // 3. Get logs from exactly one week ago for comparison
        List<WorkoutLog> previousLogs = await _repository.GetWorkoutLogForDateAsync(
            selectedDate.Date.AddDays(-7)
        );

        List<WorkoutLog> result = [];

        // 4. Reconcile logs with the routine to maintain order and fill missing ones
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
                // Create a placeholder if no log exists for this exercise today
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
            // Only save if some values are entered (to avoid cluttering DB with empty placeholders)
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
