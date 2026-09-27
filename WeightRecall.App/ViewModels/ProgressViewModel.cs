using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using Microsoft.Extensions.Logging;
using WeightRecall.Abstractions;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

[QueryProperty(nameof(ExerciseName), "ExerciseName")]
public partial class ProgressViewModel(
    ExerciseProgressService exerciseProgressService,
    IChartService chartService,
    IDialogService dialogService,
    ILogger<ProgressViewModel> logger
) : ObservableObject
{
    private readonly ExerciseProgressService _exerciseProgressService = exerciseProgressService;
    private readonly IChartService _chartService = chartService;
    private readonly IDialogService _dialogService = dialogService;
    private readonly ILogger<ProgressViewModel> _logger = logger;

    [ObservableProperty]
    private string _exerciseName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryAvailable))]
    [NotifyPropertyChangedFor(nameof(IsHistoryUnavailable))]
    private Chart? _progressChart;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoryUnavailable))]
    private bool _isBusy;

    public bool IsHistoryAvailable => ProgressChart != null;

    /// <summary>
    /// True once loading has finished without producing a chart.
    /// </summary>
    /// <remarks>
    /// Depends on BOTH properties, so both must announce changes. While only ProgressChart did,
    /// the empty-state message showed underneath the spinner for the whole of every load.
    /// </remarks>
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
                await _exerciseProgressService.GetExerciseProgressHistoryAsync(ExerciseName);

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
            _logger.LogError(ex, "Failed to load progress for {Exercise}", ExerciseName);
            await _dialogService.ShowBriefMessageAsync(
                "Could not load the progress chart.",
                isError: true
            );
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
