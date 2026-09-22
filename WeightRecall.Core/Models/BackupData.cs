using System.Text.Json.Serialization;

namespace WeightRecall.Models;

/// <summary>
/// Everything a backup file carries: the routine and the recorded exercise logs.
/// </summary>
/// <remarks>
/// This is the app's wire format. The property names below are pinned because a file exported by
/// an older build must still restore, and a mismatch does not fail loudly -- the affected section
/// simply comes back empty.
/// </remarks>
public sealed class BackupData
{
    /// <summary>
    /// The format version this build writes. Nothing reads it back yet.
    /// </summary>
    public int Version { get; set; } = 1;

    public DateTime ExportedAt { get; set; }

    /// <summary>
    /// Pinned pre-rename name. The text format uses it as a section header too.
    /// </summary>
    [JsonPropertyName("RoutineItems")]
    public List<PlannedExerciseBackup> PlannedExercises { get; set; } = [];

    /// <summary>
    /// Pinned pre-rename name, as above.
    /// </summary>
    [JsonPropertyName("WorkoutLogs")]
    public List<ExerciseLogBackup> ExerciseLogs { get; set; } = [];

    /// <summary>
    /// Whether this backup carries anything at all. Restoring replaces existing data, so an
    /// empty payload erases everything the user has recorded.
    /// </summary>
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

    /// <summary>
    /// Pinned pre-rename name of <see cref="PlannedExercise.Position"/>.
    /// </summary>
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
