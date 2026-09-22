using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for the main page, managing daily exercise logs and weekly navigation.
/// </summary>
/// <remarks>
/// Prompts, navigation and the current date all arrive through injected services rather than
/// being reached for directly, so the rules here -- which week can be shown, when a deletion
/// needs confirming, when a command is allowed to run -- can be exercised without a running app.
/// </remarks>
public partial class MainViewModel : ObservableObject
{
    private readonly ExerciseLogService _exerciseLogService;
    private readonly DateService _dateService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// Gets the collection of logged exercises for the selected date.
    /// </summary>
    public ObservableCollection<ExerciseLog> TodayExercises { get; } = [];

    /// <summary>
    /// Gets the collection of dates for the current week.
    /// </summary>
    public ObservableCollection<DateTime> WeekDays { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected date for viewing/logging workouts.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotToday))]
    private DateTime _selectedDate;

    /// <summary>
    /// True when the selected date is not today, used to show the "Today" button.
    /// </summary>
    public bool IsNotToday => SelectedDate.Date != Today;

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
    /// The current date, read through the injected clock so a test can pin it.
    /// </summary>
    private DateTime Today => _timeProvider.GetLocalNow().Date;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// Only sets up the week strip. Loading the day's exercises is left to the view's appearing
    /// event, which asks for it anyway -- doing it here as well started a second load that the
    /// <see cref="IsBusy"/> guard then discarded, and which nothing could await.
    /// </remarks>
    /// <param name="exerciseLogService">Service for exercise logs.</param>
    /// <param name="dateService">Service for date utilities.</param>
    /// <param name="navigationService">Service for moving between screens.</param>
    /// <param name="dialogService">Service for prompting the user.</param>
    /// <param name="timeProvider">Clock used to resolve today's date.</param>
    /// <param name="logger">Logger instance.</param>
    public MainViewModel(
        ExerciseLogService exerciseLogService,
        DateService dateService,
        INavigationService navigationService,
        IDialogService dialogService,
        TimeProvider timeProvider,
        ILogger<MainViewModel> logger
    )
    {
        _exerciseLogService = exerciseLogService;
        _dateService = dateService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _timeProvider = timeProvider;
        _logger = logger;

        _selectedDate = Today;
        _currentWeekMonday = _dateService.GetMonday(Today);
        GenerateWeek();
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
        if (nextMonday <= _dateService.GetMonday(Today))
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
    /// <param name="log">The exercise log containing the exercise name.</param>
    /// <returns>A task representing the asynchronous navigation.</returns>
    [RelayCommand]
    public async Task ViewProgress(ExerciseLog log)
    {
        await _navigationService.GoToExerciseProgressAsync(log.ExerciseName);
    }

    /// <summary>
    /// Command to delete an exercise log entry.
    /// </summary>
    /// <remarks>
    /// A row that was never saved has nothing to delete, so it is dropped from the list without
    /// asking; only a stored row is worth a confirmation.
    /// </remarks>
    /// <param name="log">The log entry to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task DeleteLog(ExerciseLog log)
    {
        if (log.Id != 0)
        {
            bool confirm = await _dialogService.ConfirmAsync(
                "Delete",
                $"Are you sure you want to delete {log.ExerciseName} for this day?",
                "Yes",
                "No"
            );
            if (confirm)
            {
                _ = await _exerciseLogService.DeleteExerciseLog(log);
                _ = TodayExercises.Remove(log);
            }
        }
        else
        {
            _ = TodayExercises.Remove(log);
        }
    }

    /// <summary>
    /// Command to jump back to today's date and scroll the week view to the current week.
    /// </summary>
    [RelayCommand]
    public async Task GoToToday()
    {
        CurrentWeekMonday = _dateService.GetMonday(Today);
        GenerateWeek();
        await SelectDate(Today);
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
            await _exerciseLogService.SaveExerciseLogsAsync(TodayExercises);
            await _dialogService.AlertAsync("Saved", "Recent workout progress has been saved.");
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

            List<ExerciseLog> logs = await _exerciseLogService.GetDailyExerciseLogsAsync(
                SelectedDate
            );
            foreach (ExerciseLog log in logs)
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
