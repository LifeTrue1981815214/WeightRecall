using System.Text.Json.Serialization;

namespace WeightRecall.Models;

/// <summary>
/// Everything a backup file carries: the routine and the recorded exercise logs.
/// </summary>
/// <remarks>
/// This is the app's wire format, so the property names below are not free to change.
/// Several of them predate the renames to PlannedExercise and Position and are pinned with
/// explicit attributes, because a file exported by an older build must still restore correctly.
/// A mismatch here does not fail loudly -- the affected section simply comes back empty.
/// </remarks>
public sealed class BackupData
{
    /// <summary>
    /// Format version of the file.
    /// </summary>
    /// <remarks>
    /// Written on export. Nothing reads it back yet, so a file from a future build is restored
    /// as though it were this version.
    /// TODO: check this on import, so a newer format can be rejected instead of misread.
    /// </remarks>
    public int Version { get; set; } = 1;

    public DateTime ExportedAt { get; set; }

    // "RoutineItems" is the on-disk name from before the rename to PlannedExercise, and is
    // pinned on purpose: it is the wire format of every backup users have already exported.
    // The text format pins the same name as a section header; see BackupTextFormat.
    // TODO: unpin when the backup format gets a version bump that can migrate old files.
    [JsonPropertyName("RoutineItems")]
    public List<PlannedExerciseBackup> PlannedExercises { get; set; } = [];

    // Pinned for the same reason as PlannedExercises above: "WorkoutLogs" is the name in every
    // backup users have already exported, and the text section header matches it.
    // TODO: unpin when the backup format gets a version bump that can migrate old files.
    [JsonPropertyName("WorkoutLogs")]
    public List<ExerciseLogBackup> ExerciseLogs { get; set; } = [];

    /// <summary>
    /// Gets a value indicating whether this backup carries anything at all.
    /// </summary>
    /// <remarks>
    /// Restoring replaces the existing data, so an empty payload would erase everything the
    /// user has recorded. Nothing checks this yet.
    /// TODO: refuse to restore an empty backup rather than treating it as "delete it all".
    /// </remarks>
    [JsonIgnore]
    public bool IsEmpty => PlannedExercises.Count == 0 && ExerciseLogs.Count == 0;
}

/// <summary>
/// One planned exercise as it appears in a backup file.
/// </summary>
public sealed class PlannedExerciseBackup
{
    public string ExerciseName { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }

    // Pinned for the same reason as BackupData.PlannedExercises: "Order" is the name in every
    // backup users have already exported, and the text column header matches it.
    // TODO: unpin when the backup format gets a version bump that can migrate old files.
    [JsonPropertyName("Order")]
    public int Position { get; set; }
}

/// <summary>
/// One recorded exercise log as it appears in a backup file.
/// </summary>
public sealed class ExerciseLogBackup
{
    public DateTime Date { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public double Weight { get; set; }
}
