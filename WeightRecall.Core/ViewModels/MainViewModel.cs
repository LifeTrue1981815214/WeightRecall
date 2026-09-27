using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Abstractions;
using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for the main page, managing daily exercise logs and weekly navigation.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ExerciseLogService _exerciseLogService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MainViewModel> _logger;

    /// <summary>
    /// Logged exercises for the selected date.
    /// </summary>
    public ObservableCollection<ExerciseLog> TodayExercises { get; } = [];

    /// <summary>
    /// The seven dates shown in the week strip.
    /// </summary>
    public ObservableCollection<DateTime> WeekDays { get; } = [];

    /// <summary>
    /// The date being viewed or logged against.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotToday))]
    private DateTime _selectedDate;

    /// <summary>
    /// True when the selected date is not today; shows the "Today" button.
    /// </summary>
    public bool IsNotToday => SelectedDate.Date != Today;

    /// <summary>
    /// Monday of the week the strip is showing.
    /// </summary>
    [ObservableProperty]
    private DateTime _currentWeekMonday;

    /// <summary>
    /// True while an async operation is running; guards commands against re-entry.
    /// </summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Today, read through the injected clock so a test can pin it.
    /// </summary>
    private DateTime Today => _timeProvider.GetLocalNow().Date;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// Sets up the week strip only. The day's exercises are loaded by the view's appearing
    /// event; doing it here too started a second load that nothing could await.
    /// </remarks>
    public MainViewModel(
        ExerciseLogService exerciseLogService,
        INavigationService navigationService,
        IDialogService dialogService,
        TimeProvider timeProvider,
        ILogger<MainViewModel> logger
    )
    {
        _exerciseLogService = exerciseLogService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _timeProvider = timeProvider;
        _logger = logger;

        _selectedDate = Today;
        _currentWeekMonday = WeekCalendar.GetMonday(Today);
        GenerateWeek();
    }

    /// <summary>
    /// Fills <see cref="WeekDays"/> from <see cref="CurrentWeekMonday"/>.
    /// </summary>
    private void GenerateWeek()
    {
        WeekDays.Clear();
        List<DateTime> days = WeekCalendar.GetDaysOfWeek(CurrentWeekMonday);
        foreach (DateTime day in days)
        {
            WeekDays.Add(day);
        }
    }

    /// <summary>
    /// Steps the week strip back seven days.
    /// </summary>
    [RelayCommand]
    public void PreviousWeek()
    {
        CurrentWeekMonday = CurrentWeekMonday.AddDays(-7);
        GenerateWeek();
    }

    /// <summary>
    /// Steps the week strip forward, never past the current week.
    /// </summary>
    [RelayCommand]
    public void NextWeek()
    {
        DateTime nextMonday = CurrentWeekMonday.AddDays(7);
        if (nextMonday <= WeekCalendar.GetMonday(Today))
        {
            CurrentWeekMonday = nextMonday;
            GenerateWeek();
        }
    }

    /// <summary>
    /// Selects a date and loads its exercises.
    /// </summary>
    [RelayCommand]
    public async Task SelectDate(DateTime date)
    {
        SelectedDate = date;
        await LoadTodayExercises();
    }

    /// <summary>
    /// Opens the progress chart for this exercise.
    /// </summary>
    [RelayCommand]
    public async Task ViewProgress(ExerciseLog log)
    {
        await _navigationService.GoToExerciseProgressAsync(log.ExerciseName);
    }

    /// <summary>
    /// Deletes a logged entry. An unsaved row has nothing to delete, so it is dropped from the
    /// list without asking.
    /// </summary>
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
    /// Jumps back to today and to the current week.
    /// </summary>
    [RelayCommand]
    public async Task GoToToday()
    {
        CurrentWeekMonday = WeekCalendar.GetMonday(Today);
        GenerateWeek();
        await SelectDate(Today);
    }

    /// <summary>
    /// Writes the day's entries and reports the outcome either way.
    /// </summary>
    /// <remarks>
    /// A failure used to escape uncaught, so the confirmation never appeared and nothing took
    /// its place: the button simply stopped being busy and the user was left believing their
    /// workout had been recorded.
    /// </remarks>
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
            await _dialogService.ShowBriefMessageAsync("Workout saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save exercise logs for {SelectedDate}", SelectedDate);
            await _dialogService.ShowBriefMessageAsync(
                "Could not save your workout. Please try again.",
                isError: true
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
            _logger.LogError(ex, "Failed to load exercises for {SelectedDate}", SelectedDate);
            await _dialogService.ShowBriefMessageAsync(
                "Could not load this day's exercises.",
                isError: true
            );
        }
        finally
        {
            IsBusy = false;
        }
    }
}
