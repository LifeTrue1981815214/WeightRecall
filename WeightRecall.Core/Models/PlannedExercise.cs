using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// Represents an exercise planned for a given day within the weekly workout routine.
/// </summary>
/// <remarks>
/// The table name predates the rename from "RoutineItem" and is pinned deliberately:
/// changing it would orphan the rows in every already-installed copy of the app.
/// </remarks>
[Table("RoutineItems")]
public class PlannedExercise
{
    /// <summary>
    /// Gets or sets the unique identifier for the planned exercise.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the exercise.
    /// </summary>
    public string ExerciseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the day of the week this exercise is performed.
    /// </summary>
    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>
    /// Gets or sets the position of the exercise within that day, starting at 1.
    /// </summary>
    public int Order { get; set; }
}
