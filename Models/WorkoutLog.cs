using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WeightRecall.Models;

public partial class WorkoutLog : ObservableObject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    [ObservableProperty]
    private int _sets;

    [ObservableProperty]
    private int _reps;

    [ObservableProperty]
    private double _weight;

    [ObservableProperty]
    [property: Ignore]
    private string _previousDescription = string.Empty;
}
