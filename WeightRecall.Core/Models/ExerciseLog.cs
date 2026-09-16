using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// Represents a logged entry for a specific exercise performed during a workout.
/// </summary>
[Table("WorkoutLogs")]
public partial class WorkoutLog : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the workout log entry.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the date the exercise was performed.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the name of the exercise performed.
    /// </summary>
    public string ExerciseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of sets performed.
    /// </summary>
    [ObservableProperty]
    private int _sets;

    /// <summary>
    /// Gets or sets the number of repetitions performed per set.
    /// </summary>
    [ObservableProperty]
    private int _reps;

    /// <summary>
    /// Gets or sets the weight lifted.
    /// </summary>
    [ObservableProperty]
    private double _weight;

    /// <summary>
    /// Gets or sets a description of the previous workout performance for this exercise.
    /// This property is not stored in the database.
    /// </summary>
    [ObservableProperty]
    [property: Ignore]
    private string _previousDescription = string.Empty;
}
