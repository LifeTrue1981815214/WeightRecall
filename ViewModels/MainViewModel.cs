using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly WorkoutLogService _workoutLogService;
    private readonly DateService _dateService;
    private readonly ILogger<MainViewModel> _logger;

    public ObservableCollection<WorkoutLog> TodayExercises { get; } = [];
    public ObservableCollection<DateTime> WeekDays { get; } = [];

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _currentWeekMonday;

    [ObservableProperty]
    private bool _isBusy;

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

    private void GenerateWeek()
    {
        WeekDays.Clear();
        List<DateTime> days = _dateService.GetDaysOfWeek(CurrentWeekMonday);
        foreach (DateTime day in days)
        {
            WeekDays.Add(day);
        }
    }

    [RelayCommand]
    public void PreviousWeek()
    {
        CurrentWeekMonday = CurrentWeekMonday.AddDays(-7);
        GenerateWeek();
    }

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

    [RelayCommand]
    public async Task SelectDate(DateTime date)
    {
        SelectedDate = date;
        await LoadTodayExercises();
    }

    [RelayCommand]
    public async Task ViewProgress(WorkoutLog log)
    {
        await Shell.Current.GoToAsync($"{nameof(ProgressPage)}?ExerciseName={log.ExerciseName}");
    }

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
