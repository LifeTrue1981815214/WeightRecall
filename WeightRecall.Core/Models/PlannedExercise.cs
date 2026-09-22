using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// An exercise planned for a given day within the weekly routine.
/// </summary>
/// <remarks>
/// The table and column names predate the renames to PlannedExercise and Position and are
/// pinned: sqlite-net derives them from the C# names, so changing them would orphan the rows in
/// every installed copy of the app.
/// </remarks>
[Table("RoutineItems")]
public class PlannedExercise
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Position within the day, starting at 1.
    /// </summary>
    [Column("Order")]
    public int Position { get; set; }
}
