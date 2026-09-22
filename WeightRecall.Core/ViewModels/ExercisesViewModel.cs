using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for managing the workout routine exercises.
/// Allows adding, editing, and deleting planned exercises for different days of the week.
/// </summary>
/// <remarks>
/// Prompting, navigating, marshalling to the UI thread and reading the current day all arrive
/// through injected services rather than being reached for directly, so the rules here -- add
/// versus edit, what a cancelled deletion leaves behind, when a command may run -- can be
/// exercised without a running app.
/// </remarks>
public partial class ExercisesViewModel : ObservableObject
{
    private readonly PlannedExerciseService _plannedExerciseService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<ExercisesViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExercisesViewModel"/> class.
    /// </summary>
    /// <remarks>
    /// Only picks the day to start on. Loading that day's exercises is left to the view's
    /// appearing event, which asks for it anyway -- doing it here as well started a second load
    /// that nothing could await and that the first one raced.
    /// </remarks>
    /// <param name="plannedExerciseService">Service for planned exercise business logic.</param>
    /// <param name="navigationService">Service for moving between screens.</param>
    /// <param name="dialogService">Service for prompting the user.</param>
    /// <param name="uiDispatcher">Marshals collection updates onto the UI thread.</param>
    /// <param name="timeProvider">Clock used to resolve today's weekday.</param>
    /// <param name="logger">The logger instance for diagnostics.</param>
    public ExercisesViewModel(
        PlannedExerciseService plannedExerciseService,
        INavigationService navigationService,
        IDialogService dialogService,
        TimeProvider timeProvider,
        ILogger<ExercisesViewModel> logger
    )
    {
        _plannedExerciseService = plannedExerciseService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _logger = logger;

        // Assigned to the field rather than the property on purpose: setting the property would
        // fire OnSelectedDayChanged and start a load before the view is ready for one.
        _selectedDay = timeProvider.GetLocalNow().DayOfWeek;
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
        NewPosition = exercise.Position.ToString();
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
        NewPosition = string.Empty;
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

            PlannedExercises.Clear();
            foreach (PlannedExercise exercise in exercises)
            {
                PlannedExercises.Add(exercise);
            }
            _logger.LogInformation("Loaded {Count} planned exercises", exercises.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load planned exercises for {Day}", SelectedDay);
            await _dialogService.AlertAsync("Error", $"Failed to load routine: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ViewProgress(PlannedExercise exercise)
    {
        await _navigationService.GoToExerciseProgressAsync(exercise.ExerciseName);
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
            bool answer = await _dialogService.ConfirmAsync(
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
                await _dialogService.AlertAsync(
                    "Exercise Removed",
                    "The planned exercise had been deleted"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete planned exercise {Id}", exercise.Id);
            await _dialogService.AlertAsync("Error", $"Failed to delete: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public List<DayOfWeek> AvailableDays { get; } = [.. Enum.GetValues<DayOfWeek>()];

    [ObservableProperty]
    private DayOfWeek _selectedDay;

    partial void OnSelectedDayChanged(DayOfWeek value)
    {
        _ = LoadPlannedExercisesAsync();
    }

    [ObservableProperty]
    private string _newExerciseName = string.Empty;

    [ObservableProperty]
    private string _newPosition = string.Empty;

    [RelayCommand]
    private async Task SavePlannedExerciseAsync()
    {
        if (IsBusy || !int.TryParse(NewPosition, out int position))
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
                _editingExercise.Position = position;
                _editingExercise.DayOfWeek = targetDay;
                await _plannedExerciseService.SavePlannedExerciseAsync(_editingExercise, oldDay);
            }
            else
            {
                _logger.LogInformation("Adding new planned exercise: {Name}", NewExerciseName);
                PlannedExercise exercise = new()
                {
                    ExerciseName = NewExerciseName,
                    Position = position,
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
            await _dialogService.AlertAsync("Database Error", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
