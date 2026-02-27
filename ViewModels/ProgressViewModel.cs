using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for the progress page, displaying exercise performance over time using charts.
/// </summary>
/// <param name="workoutLogService">Service for workout logs.</param>
/// <param name="logger">Logger instance.</param>
[QueryProperty(nameof(ExerciseName), "ExerciseName")]
public partial class ProgressViewModel(
    WorkoutLogService workoutLogService,
    ILogger<ProgressViewModel> logger
) : ObservableObject
{
    private readonly WorkoutLogService _workoutLogService = workoutLogService;
    private readonly ILogger<ProgressViewModel> _logger = logger;

    /// <summary>
    /// Gets or sets the name of the exercise for which to display progress.
    /// This is populated via query parameter.
    /// </summary>
    [ObservableProperty]
    private string _exerciseName = string.Empty;

    /// <summary>
    /// Gets or sets the chart reflecting the progress of the exercise.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryAvailable))]
    [NotifyPropertyChangedFor(nameof(IsHistoryUnavailable))]
    private Chart? _progressChart;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy loading data.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gets a value indicating whether progress history data is available to display.
    /// </summary>
    public bool IsHistoryAvailable => ProgressChart != null;

    /// <summary>
    /// Gets a value indicating whether no progress history was found and loading has finished.
    /// </summary>
    public bool IsHistoryUnavailable => ProgressChart == null && !IsBusy;

    /// <summary>
    /// Triggered when the <see cref="ExerciseName"/> property changes.
    /// </summary>
    /// <param name="value">The new exercise name.</param>
    partial void OnExerciseNameChanged(string value)
    {
        _ = LoadProgress();
    }

    /// <summary>
    /// Command to asynchronously load progress data and generate the chart.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    private async Task LoadProgress()
    {
        if (string.IsNullOrEmpty(ExerciseName) || IsBusy)
        {
            return;
        }

        try
        {
            _logger.LogInformation("Loading progress history for {Exercise}", ExerciseName);
            IsBusy = true;
            List<ExerciseProgressPoint> history =
                await _workoutLogService.GetExerciseProgressHistoryAsync(ExerciseName);

            if (history == null || history.Count == 0)
            {
                _logger.LogInformation("No history found for {Exercise}", ExerciseName);
                ProgressChart = null;
                return;
            }

            _logger.LogInformation(
                "Found {Count} data points for {Exercise}",
                history.Count,
                ExerciseName
            );
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load progress for {Exercise}", ExerciseName);
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
