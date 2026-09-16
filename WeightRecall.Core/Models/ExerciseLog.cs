using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// Represents a logged entry for a specific exercise performed during a workout.
/// </summary>
/// <remarks>
/// The table name predates the rename to ExerciseLog and is pinned deliberately: sqlite-net
/// derives it from the C# name, so changing it would orphan the rows in every already-installed
/// copy of the app.
/// TODO: unpin this as part of a schema migration, so the stored name matches the C# one.
/// Until that migration exists, leave the attribute alone.
/// </remarks>
[Table("WorkoutLogs")]
public partial class ExerciseLog : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the exercise log entry.
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

    /// <summary>
    /// Gets a value indicating whether anything was actually recorded against this exercise.
    /// </summary>
    /// <remarks>
    /// The day's sheet hands back a row for every planned exercise, including ones the user never
    /// filled in. Those are all-zero and must not be written to the database, or every exercise
    /// would gain an empty entry on every day it appears in the routine.
    /// Not stored; derived from the other columns.
    /// </remarks>
    [Ignore]
    public bool HasRecordedActivity => Weight > 0 || Sets > 0 || Reps > 0;
}
