using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using SkiaSharp;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

[QueryProperty(nameof(ExerciseName), "ExerciseName")]
public partial class ProgressViewModel(WorkoutLogService workoutLogService) : ObservableObject
{
    private readonly WorkoutLogService _workoutLogService = workoutLogService;

    [ObservableProperty]
    private string _exerciseName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryAvailable))]
    [NotifyPropertyChangedFor(nameof(IsHistoryUnavailable))]
    private Chart? _progressChart;

    [ObservableProperty]
    private bool _isBusy;

    public bool IsHistoryAvailable => ProgressChart != null;
    public bool IsHistoryUnavailable => ProgressChart == null && !IsBusy;

    partial void OnExerciseNameChanged(string value)
    {
        _ = LoadProgress();
    }

    [RelayCommand]
    private async Task LoadProgress()
    {
        if (string.IsNullOrEmpty(ExerciseName) || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            List<ExerciseProgressPoint> history =
                await _workoutLogService.GetExerciseProgressHistoryAsync(ExerciseName);

            if (history == null || history.Count == 0)
            {
                ProgressChart = null;
                return;
            }

            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            SKColor textColor = isDark ? SKColors.White : SKColors.Black;

            List<ChartEntry> entries =
            [
                .. history.Select(p => new ChartEntry((float)p.MaxWeight)
                {
                    Label = p.Date.ToString("dd/MM"),
                    ValueLabel = p.MaxWeight.ToString("0.##"),
                    Color = SKColor.Parse("#000"),
                    TextColor = textColor,
                }),
            ];

            ProgressChart = new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Straight,
                LineSize = 8,
                PointMode = PointMode.Circle,
                PointSize = 18,
                LabelTextSize = 30,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
            };
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }
}
