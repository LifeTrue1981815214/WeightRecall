using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;
using WeightRecall.Views;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for managing the workout routine exercises.
/// Allows adding, editing, and deleting planned exercises for different days of the week.
/// </summary>
public partial class ExercisesViewModel : ObservableObject
{
    private readonly PlannedExerciseService _plannedExerciseService;
    private readonly ILogger<ExercisesViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExercisesViewModel"/> class.
    /// </summary>
    /// <param name="plannedExerciseService">Service for planned exercise business logic.</param>
    /// <param name="logger">The logger instance for diagnostics.</param>
    public ExercisesViewModel(
        PlannedExerciseService plannedExerciseService,
        ILogger<ExercisesViewModel> logger
    )
    {
        _plannedExerciseService = plannedExerciseService;
        _logger = logger;
        _selectedDay = DateTime.Today.DayOfWeek;
        _ = LoadPlannedExercisesAsync();
    }

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is performing an asynchronous operation.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>
    /// Gets a value indicating whether the ViewModel is not busy.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Gets the collection of planned exercises for the selected day.
    /// </summary>
    public ObservableCollection<PlannedExercise> PlannedExercises { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the "Add/Edit" popup is currently visible.
    /// </summary>
    [ObservableProperty]
    private bool _isAddingPlannedExercise;

    /// <summary>
    /// Gets or sets a value indicating whether the current operation is an edit (true) or an add (false).
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    private PlannedExercise? _editingExercise;

    /// <summary>
    /// Gets the title for the entry popup based on the current mode (Add/Edit).
    /// </summary>
    public string PopupTitle => IsEditing ? "Edit Exercise" : "Add New Exercise";

    /// <summary>
    /// Gets the button text for the entry popup based on the current mode (Add/Edit).
    /// </summary>
    public string PopupButtonText => IsEditing ? "Update" : "Add";

    /// <summary>
    /// Command to display the popup in "Add" mode.
    /// </summary>
    [RelayCommand]
    private void ShowAddPlannedExercise()
    {
        IsEditing = false;
        IsAddingPlannedExercise = true;
        OnPropertyChanged(nameof(PopupTitle));
        OnPropertyChanged(nameof(PopupButtonText));
    }

    /// <summary>
    /// Command to display the popup in "Edit" mode for a specific planned exercise.
    /// </summary>
    /// <param name="exercise">The planned exercise to edit.</param>
    [RelayCommand]
    private void ShowEditPlannedExercise(PlannedExercise exercise)
    {
        _editingExercise = exercise;
        NewExerciseName = exercise.ExerciseName;
        NewOrder = exercise.Order.ToString();
        SelectedDay = exercise.DayOfWeek;
        IsEditing = true;
        IsAddingPlannedExercise = true;
        OnPropertyChanged(nameof(PopupTitle));
        OnPropertyChanged(nameof(PopupButtonText));
    }

    /// <summary>
    /// Command to hide the entry popup and reset fields.
    /// </summary>
    [RelayCommand]
    private void HideAddPlannedExercise()
    {
        IsAddingPlannedExercise = false;
        IsEditing = false;
        _editingExercise = null;
        NewExerciseName = string.Empty;
        NewOrder = string.Empty;
    }

    /// <summary>
    /// Command to fetch planned exercises from the service for the currently selected day.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task LoadPlannedExercisesAsync()
    {
        try
        {
            _logger.LogInformation("Loading planned exercises for {Day}", SelectedDay);
            List<PlannedExercise> exercises =
                await _plannedExerciseService.GetPlannedExercisesForDayAsync(SelectedDay);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PlannedExercises.Clear();
                foreach (PlannedExercise exercise in exercises)
                {
                    PlannedExercises.Add(exercise);
                }
            });
            _logger.LogInformation("Loaded {Count} planned exercises", exercises.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load planned exercises for {Day}", SelectedDay);
            await Shell.Current.DisplayAlertAsync(
                "Error",
                $"Failed to load routine: {ex.Message}",
                "OK"
            );
        }
    }

    [RelayCommand]
    private async Task ViewProgress(PlannedExercise exercise)
    {
        await Shell.Current.GoToAsync(
            $"{nameof(ProgressPage)}?ExerciseName={exercise.ExerciseName}"
        );
    }

    [RelayCommand]
    private async Task DeletePlannedExerciseAsync(PlannedExercise exercise)
    {
        if (exercise == null || IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            bool answer = await Shell.Current.DisplayAlertAsync(
                "Do you want to remove this item?",
                "Are you sure? This cannot be undone.",
                "Delete",
                "Cancel"
            );
            if (answer)
            {
                _logger.LogInformation(
                    "Deleting planned exercise {Id} - {Name}",
                    exercise.Id,
                    exercise.ExerciseName
                );
                _ = await _plannedExerciseService.DeletePlannedExerciseAsync(exercise);
                await LoadPlannedExercisesAsync();
                await Shell.Current.DisplayAlertAsync(
                    "Exercise Removed",
                    "The planned exercise had been deleted",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete planned exercise {Id}", exercise.Id);
            await Shell.Current.DisplayAlertAsync("Error", $"Failed to delete: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public List<DayOfWeek> AvailableDays { get; } =
    [.. Enum.GetValues<DayOfWeek>().Cast<DayOfWeek>()];

    [ObservableProperty]
    private DayOfWeek _selectedDay;

    partial void OnSelectedDayChanged(DayOfWeek value)
    {
        _ = LoadPlannedExercisesAsync();
    }

    [ObservableProperty]
    private string _newExerciseName = string.Empty;

    [ObservableProperty]
    private string _newOrder = string.Empty;

    [RelayCommand]
    private async Task SavePlannedExerciseAsync()
    {
        if (IsBusy || !int.TryParse(NewOrder, out int order))
        {
            return;
        }

        try
        {
            IsBusy = true;
            DayOfWeek targetDay = SelectedDay;
            DayOfWeek? oldDay = null;

            if (IsEditing && _editingExercise != null)
            {
                _logger.LogInformation("Updating planned exercise {Id}", _editingExercise.Id);
                oldDay = _editingExercise.DayOfWeek;
                _editingExercise.ExerciseName = NewExerciseName;
                _editingExercise.Order = order;
                _editingExercise.DayOfWeek = targetDay;
                await _plannedExerciseService.SavePlannedExerciseAsync(_editingExercise, oldDay);
            }
            else
            {
                _logger.LogInformation("Adding new planned exercise: {Name}", NewExerciseName);
                PlannedExercise exercise = new()
                {
                    ExerciseName = NewExerciseName,
                    Order = order,
                    DayOfWeek = targetDay,
                };
                await _plannedExerciseService.SavePlannedExerciseAsync(exercise);
            }

            await LoadPlannedExercisesAsync();
            HideAddPlannedExercise();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save planned exercise");
            await Shell.Current.DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
