using WeightRecall.Data;
using WeightRecall.Models;

namespace WeightRecall.Services;

/// <summary>
/// Moves data between the database and a <see cref="BackupData"/> payload.
/// </summary>
/// <remarks>
/// Deals only in whole-database contents; writing a payload to a file belongs to the caller.
/// </remarks>
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
    /// Destructive, and not one unit of work: existing rows are deleted before the new ones are
    /// written, and an empty payload is accepted as an instruction to delete everything.
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
