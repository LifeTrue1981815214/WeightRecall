using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Repository;

public class WorkoutLogRepository(DatabaseContext context, ILogger<WorkoutLogRepository> logger)
{
    private readonly DatabaseContext _context = context;
    private readonly ILogger<WorkoutLogRepository> _logger = logger;

    private async Task<SQLite.SQLiteAsyncConnection> GetConnectionAsync()
    {
        await _context.InitializeAsync();
        return _context.Connection;
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogsAsync()
    {
        try
        {
            return await (await GetConnectionAsync()).Table<WorkoutLog>().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workout logs");
            throw;
        }
    }

    public async Task<List<WorkoutLog>> GetWorkoutLogForDateAsync(DateTime date)
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<WorkoutLog>()
                .Where(r => r.Date == date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get workout logs for {Date}", date);
            throw;
        }
    }

    public async Task<int> SaveWorkoutLogAsync(WorkoutLog item)
    {
        try
        {
            SQLite.SQLiteAsyncConnection connection = await GetConnectionAsync();
            if (item.Id == 0)
            {
                _logger.LogInformation(
                    "Inserting new workout log for {Exercise}",
                    item.ExerciseName
                );
                return await connection.InsertAsync(item);
            }
            else
            {
                _logger.LogInformation(
                    "Updating workout log {Id} for {Exercise}",
                    item.Id,
                    item.ExerciseName
                );
                return await connection.UpdateAsync(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workout log for {Exercise}", item.ExerciseName);
            throw;
        }
    }

    public async Task<int> DeleteWorkoutLogAsync(WorkoutLog item)
    {
        try
        {
            _logger.LogInformation("Deleting workout log {Id}", item.Id);
            return await (await GetConnectionAsync()).DeleteAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete workout log {Id}", item.Id);
            throw;
        }
    }

    public async Task<List<WorkoutLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    )
    {
        try
        {
            return await (await GetConnectionAsync())
                .Table<WorkoutLog>()
                .Where(w =>
                    w.ExerciseName == exerciseName && w.Date >= startDate && w.Date <= endDate
                )
                .OrderBy(w => w.Date)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get logs for {Exercise} between {Start} and {End}",
                exerciseName,
                startDate,
                endDate
            );
            throw;
        }
    }
}
