using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Services;

/// <summary>
/// Moves data between the database and a <see cref="BackupData"/> payload.
/// </summary>
/// <remarks>
/// This deals only in whole-database contents; turning a payload into a file, and picking which
/// file, belongs to the caller.
/// </remarks>
/// <param name="context">The database context for data access.</param>
public class BackupService(DatabaseContext context)
{
    private readonly DatabaseContext _context = context;

    /// <summary>
    /// Reads everything currently stored into a backup payload.
    /// </summary>
    public async Task<BackupData> BuildAsync()
    {
        await _context.InitializeAsync();

        List<PlannedExercise> plannedExercises = await _context
            .Connection.Table<PlannedExercise>()
            .ToListAsync();
        List<ExerciseLog> exerciseLogs = await _context
            .Connection.Table<ExerciseLog>()
            .ToListAsync();

        return new BackupData
        {
            ExportedAt = DateTime.UtcNow,
            PlannedExercises =
            [
                .. plannedExercises.Select(e => new PlannedExerciseBackup
                {
                    ExerciseName = e.ExerciseName,
                    DayOfWeek = e.DayOfWeek,
                    Position = e.Position,
                }),
            ],
            ExerciseLogs =
            [
                .. exerciseLogs.Select(w => new ExerciseLogBackup
                {
                    Date = w.Date,
                    ExerciseName = w.ExerciseName,
                    Sets = w.Sets,
                    Reps = w.Reps,
                    Weight = w.Weight,
                }),
            ],
        };
    }

    /// <summary>
    /// Replaces everything currently stored with the contents of a backup payload.
    /// </summary>
    /// <remarks>
    /// Destructive: the existing rows are deleted before the new ones are written.
    /// TODO: the delete and the inserts are not one unit of work, so a failure partway through
    /// leaves the user with neither their old data nor the restored data. And an empty payload
    /// is accepted as an instruction to delete everything; see <see cref="BackupData.IsEmpty"/>.
    /// </remarks>
    public async Task ApplyAsync(BackupData data)
    {
        await _context.InitializeAsync();
        _ = await _context.Connection.DeleteAllAsync<ExerciseLog>();
        _ = await _context.Connection.DeleteAllAsync<PlannedExercise>();

        _ = await _context.Connection.InsertAllAsync(
            data.PlannedExercises.Select(e => new PlannedExercise
            {
                ExerciseName = e.ExerciseName,
                DayOfWeek = e.DayOfWeek,
                Position = e.Position,
            })
        );

        _ = await _context.Connection.InsertAllAsync(
            data.ExerciseLogs.Select(w => new ExerciseLog
            {
                Date = w.Date,
                ExerciseName = w.ExerciseName,
                Sets = w.Sets,
                Reps = w.Reps,
                Weight = w.Weight,
            })
        );
    }
}
