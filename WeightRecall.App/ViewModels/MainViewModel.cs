using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;
using WeightRecall.Views;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for the main page, managing daily workout logs and weekly navigation.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly WorkoutLogService _workoutLogService;
    private readonly DateService _dateService;
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// Gets the collection of logged exercises for the selected date.
    /// </summary>
    public ObservableCollection<WorkoutLog> TodayExercises { get; } = [];

    /// <summary>
    /// Gets the collection of dates for the current week.
    /// </summary>
    public ObservableCollection<DateTime> WeekDays { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected date for viewing/logging workouts.
    /// </summary>
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    /// <summary>
    /// Gets or sets the Monday of the current week being displayed.
    /// </summary>
    [ObservableProperty]
    private DateTime _currentWeekMonday;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is performing an asynchronous operation.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="workoutLogService">Service for workout logs.</param>
    /// <param name="dateService">Service for date utilities.</param>
    /// <param name="logger">Logger instance.</param>
    public MainViewModel(
        WorkoutLogService workoutLogService,
        DateService dateService,
        ILogger<MainViewModel> logger
    )
    {
        _workoutLogService = workoutLogService;
        _dateService = dateService;
        _logger = logger;
        _currentWeekMonday = _dateService.GetMonday(DateTime.Today);
        GenerateWeek();
        _ = LoadTodayExercises();
    }

    /// <summary>
    /// Populates the <see cref="WeekDays"/> collection based on <see cref="CurrentWeekMonday"/>.
    /// </summary>
    private void GenerateWeek()
    {
        WeekDays.Clear();
        List<DateTime> days = _dateService.GetDaysOfWeek(CurrentWeekMonday);
        foreach (DateTime day in days)
        {
            WeekDays.Add(day);
        }
    }

    /// <summary>
    /// Command to navigate to the previous week.
    /// </summary>
    [RelayCommand]
    public void PreviousWeek()
    {
        CurrentWeekMonday = CurrentWeekMonday.AddDays(-7);
        GenerateWeek();
    }

    /// <summary>
    /// Command to navigate to the next week (up to current week).
    /// </summary>
    [RelayCommand]
    public void NextWeek()
    {
        DateTime nextMonday = CurrentWeekMonday.AddDays(7);
        if (nextMonday <= _dateService.GetMonday(DateTime.Today))
        {
            CurrentWeekMonday = nextMonday;
            GenerateWeek();
        }
    }

    /// <summary>
    /// Command to select a specific date and load its exercises.
    /// </summary>
    /// <param name="date">The date to select.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task SelectDate(DateTime date)
    {
        SelectedDate = date;
        await LoadTodayExercises();
    }

    /// <summary>
    /// Command to navigate to the progress chart for a specific exercise.
    /// </summary>
    /// <param name="log">The workout log containing the exercise name.</param>
    /// <returns>A task representing the asynchronous navigation.</returns>
    [RelayCommand]
    public async Task ViewProgress(WorkoutLog log)
    {
        await Shell.Current.GoToAsync($"{nameof(ProgressPage)}?ExerciseName={log.ExerciseName}");
    }

    /// <summary>
    /// Command to delete a workout log entry.
    /// </summary>
    /// <param name="log">The log entry to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task DeleteLog(WorkoutLog log)
    {
        if (log.Id != 0)
        {
            bool confirm = await Shell.Current.DisplayAlertAsync(
                "Delete",
                $"Are you sure you want to delete {log.ExerciseName} for this day?",
                "Yes",
                "No"
            );
            if (confirm)
            {
                _ = await _workoutLogService.DeleteWorkoutLog(log);
                _ = TodayExercises.Remove(log);
            }
        }
        else
        {
            _ = TodayExercises.Remove(log);
        }
    }

    [RelayCommand]
    public async Task SaveLogs()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _workoutLogService.SaveWorkoutLogsAsync(TodayExercises);
            await Shell.Current.DisplayAlertAsync(
                "Saved",
                "Recent workout progress has been saved.",
                "OK"
            );
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task LoadTodayExercises()
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _logger.LogInformation("Loading exercises for {SelectedDate}", SelectedDate);
            TodayExercises.Clear();

            List<WorkoutLog> logs = await _workoutLogService.GetDailyWorkoutLogsAsync(SelectedDate);
            foreach (WorkoutLog log in logs)
            {
                TodayExercises.Add(log);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading today's exercises");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
