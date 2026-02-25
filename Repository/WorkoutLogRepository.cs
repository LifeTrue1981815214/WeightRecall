using System.Diagnostics;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

public class WorkoutLogRepository(DatabaseContext context)
{
    private readonly DatabaseContext _context = context;

    public async Task<List<WorkoutLog>> GetWorkoutLogsAsync()
    {
        await _context.InitializeAsync();
        List<WorkoutLog> items = await _context.Connection.Table<WorkoutLog>().ToListAsync();
        foreach (WorkoutLog item in items)
        {
            Debug.WriteLine(
                $"[DB DEBUG] ID: {item.Id}, Date: {item.Date}, Exercise: {item.ExerciseName}, Sets: {item.Sets}, Reps: {item.Reps}, Weight: {item.Weight}"
            );
        }
        return items;
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogForDateAsync(DateTime date)
    {
        await _context.InitializeAsync();
        List<WorkoutLog> items = await _context
            .Connection.Table<WorkoutLog>()
            .Where(r => r.Date == date)
            .ToListAsync();
        foreach (WorkoutLog item in items)
        {
            Debug.WriteLine(
                $"[DB DEBUG] ID: {item.Id}, Date: {item.Date}, Exercise: {item.ExerciseName}, Sets: {item.Sets}, Reps: {item.Reps}, Weight: {item.Weight}"
            );
        }
        return items;
    }

    public async Task<int> SaveWorkoutLogAsync(WorkoutLog item)
    {
        await _context.InitializeAsync();
        return item.Id == 0
            ? await _context.Connection.InsertAsync(item)
            : await _context.Connection.UpdateAsync(item);
    }

    public async Task<int> DeleteWorkoutLogAsync(WorkoutLog item)
    {
        await _context.InitializeAsync();
        return await _context.Connection.DeleteAsync(item);
    }

    public async Task<List<WorkoutLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    )
    {
        await _context.InitializeAsync();
        return await _context
            .Connection.Table<WorkoutLog>()
            .Where(w => w.ExerciseName == exerciseName && w.Date >= startDate && w.Date <= endDate)
            .OrderBy(w => w.Date)
            .ToListAsync();
    }
}
