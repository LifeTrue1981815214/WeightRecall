using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WeightRecall.Models;

/// <summary>
/// A logged entry for a specific exercise performed during a workout.
/// </summary>
/// <remarks>
/// The table name predates the rename to ExerciseLog and is pinned: sqlite-net derives it from
/// the C# name, so changing it would orphan the rows in every installed copy of the app.
/// </remarks>
[Table(TableName)]
public partial class ExerciseLog : ObservableObject
{
    /// <summary>
    /// The name this entity is stored under. Shared by the mapping attribute and by raw SQL that
    /// names the table, so the two cannot drift apart.
    /// </summary>
    public const string TableName = "WorkoutLogs";

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// The date the exercise was performed.
    /// </summary>
    public DateTime Date { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    private int _sets;

    [ObservableProperty]
    private int _reps;

    [ObservableProperty]
    private double _weight;

    /// <summary>
    /// How the same exercise went last time, shown under its name. Not stored.
    /// </summary>
    [ObservableProperty]
    [property: Ignore]
    private string _previousDescription = string.Empty;

    /// <summary>
    /// Whether anything was actually recorded against this exercise.
    /// </summary>
    /// <remarks>
    /// The day's sheet hands back a row for every planned exercise, including untouched ones.
    /// Those are all-zero and must not be saved, or every exercise would gain an empty entry on
    /// every day it appears in the routine.
    /// </remarks>
    [Ignore]
    public bool HasRecordedActivity => Weight > 0 || Sets > 0 || Reps > 0;
}
