using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

[QueryProperty(nameof(ExerciseName), "ExerciseName")]
public partial class ProgressViewModel(
    WorkoutLogService workoutLogService,
    IChartService chartService,
    ILogger<ProgressViewModel> logger
) : ObservableObject
{
    private readonly WorkoutLogService _workoutLogService = workoutLogService;
    private readonly IChartService _chartService = chartService; // New Service
    private readonly ILogger<ProgressViewModel> _logger = logger;

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
            _logger.LogInformation("Loading progress for {Exercise}", ExerciseName);
            IsBusy = true;

            List<ExerciseProgressPoint> history =
                await _workoutLogService.GetExerciseProgressHistoryAsync(ExerciseName);

            if (history == null || history.Count == 0)
            {
                ProgressChart = null;
                return;
            }

            AppTheme theme = Application.Current?.RequestedTheme ?? AppTheme.Light;

            ProgressChart = _chartService.GenerateLineChart(history, theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load progress");
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
