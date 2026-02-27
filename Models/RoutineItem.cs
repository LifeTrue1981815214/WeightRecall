using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// Represents an exercise within a weekly workout routine.
/// </summary>
[Table("RoutineItems")]
public class RoutineItem
{
    /// <summary>
    /// Gets or sets the unique identifier for the routine item.
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
    /// Gets or sets the display order of the exercise within the routine for a specific day.
    /// </summary>
    public int Order { get; set; }
}
