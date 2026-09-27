using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using WeightRecall.Abstractions;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels;

/// <summary>
/// ViewModel for managing the workout routine exercises.
/// Allows adding, editing, and deleting planned exercises for different days of the week.
/// </summary>
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
    /// Picks the starting day only. That day's exercises are loaded by the view's appearing
    /// event; doing it here too started a second load that nothing could await.
    /// </remarks>
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

        // Field, not the property: setting the property fires OnSelectedDayChanged and would
        // start a load before the view is ready.
        _selectedDay = timeProvider.GetLocalNow().DayOfWeek;
    }

    /// <summary>
    /// True while an async operation is running; guards commands against re-entry.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool _isBusy;

    /// <summary>
    /// Inverse of <see cref="IsBusy"/>, for binding.
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Planned exercises for the selected day.
    /// </summary>
    public ObservableCollection<PlannedExercise> PlannedExercises { get; } = [];

    /// <summary>
    /// Whether the Add/Edit popup is showing.
    /// </summary>
    [ObservableProperty]
    private bool _isAddingPlannedExercise;

    /// <summary>
    /// True when the popup is editing an existing exercise rather than adding one.
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    private PlannedExercise? _editingExercise;

    /// <summary>
    /// Popup heading for the current mode.
    /// </summary>
    public string PopupTitle => IsEditing ? "Edit Exercise" : "Add New Exercise";

    /// <summary>
    /// Popup confirm-button text for the current mode.
    /// </summary>
    public string PopupButtonText => IsEditing ? "Update" : "Add";

    /// <summary>
    /// Opens the popup in "Add" mode.
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
    /// Opens the popup in "Edit" mode, filled in from the given exercise.
    /// </summary>
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
    /// Closes the popup and clears the fields.
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
    /// Loads the selected day's planned exercises.
    /// </summary>
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
            await _dialogService.ShowBriefMessageAsync(
                "Could not load your routine.",
                isError: true
            );
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

    /// <summary>
    /// Resolves the position to save at from the order box.
    /// </summary>
    /// <remarks>
    /// Empty means "put it last" when adding and "leave it where it is" when editing. Anything
    /// else non-numeric is reported, because a save button that does nothing reads as a bug.
    /// </remarks>
    /// <returns>The position to save at, or <c>null</c> if the input needs fixing.</returns>
    private async Task<int?> ReadPositionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPosition))
        {
            return IsEditing && _editingExercise is not null
                ? _editingExercise.Position
                : PlannedExercises.Count + 1;
        }

        if (int.TryParse(NewPosition, out int position))
        {
            return position;
        }

        await _dialogService.AlertAsync(
            "Invalid Order",
            $"\"{NewPosition}\" is not a number. Leave the order empty to put the exercise last."
        );
        return null;
    }

    [RelayCommand]
    private async Task SavePlannedExerciseAsync()
    {
        if (IsBusy)
        {
            return;
        }

        int? typed = await ReadPositionAsync();
        if (typed is null)
        {
            return;
        }

        int position = typed.Value;

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
