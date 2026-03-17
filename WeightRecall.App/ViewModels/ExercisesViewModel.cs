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
/// Allows adding, editing, and deleting routine items for different days of the week.
/// </summary>
public partial class ExercisesViewModel : ObservableObject
{
    private readonly RoutineService _routineService;
    private readonly ILogger<ExercisesViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExercisesViewModel"/> class.
    /// </summary>
    /// <param name="routineService">Service for routine business logic.</param>
    /// <param name="logger">The logger instance for diagnostics.</param>
    public ExercisesViewModel(RoutineService routineService, ILogger<ExercisesViewModel> logger)
    {
        _routineService = routineService;
        _logger = logger;
        _selectedDay = DateTime.Today.DayOfWeek;
        _ = LoadRoutineItemsAsync();
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
    /// Gets the collection of routine items for the selected day.
    /// </summary>
    public ObservableCollection<RoutineItem> RoutineItems { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the "Add/Edit" popup is currently visible.
    /// </summary>
    [ObservableProperty]
    private bool _isAddingRoutine;

    /// <summary>
    /// Gets or sets a value indicating whether the current operation is an edit (true) or an add (false).
    /// </summary>
    [ObservableProperty]
    private bool _isEditing;

    private RoutineItem? _editingItem;

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
    private void ShowAddRoutine()
    {
        IsEditing = false;
        IsAddingRoutine = true;
        OnPropertyChanged(nameof(PopupTitle));
        OnPropertyChanged(nameof(PopupButtonText));
    }

    /// <summary>
    /// Command to display the popup in "Edit" mode for a specific routine item.
    /// </summary>
    /// <param name="item">The routine item to edit.</param>
    [RelayCommand]
    private void ShowEditRoutine(RoutineItem item)
    {
        _editingItem = item;
        NewExerciseName = item.ExerciseName;
        NewOrder = item.Order.ToString();
        SelectedDay = item.DayOfWeek;
        IsEditing = true;
        IsAddingRoutine = true;
        OnPropertyChanged(nameof(PopupTitle));
        OnPropertyChanged(nameof(PopupButtonText));
    }

    /// <summary>
    /// Command to hide the entry popup and reset fields.
    /// </summary>
    [RelayCommand]
    private void HideAddRoutine()
    {
        IsAddingRoutine = false;
        IsEditing = false;
        _editingItem = null;
        NewExerciseName = string.Empty;
        NewOrder = string.Empty;
    }

    /// <summary>
    /// Command to fetch routine items from the service for the currently selected day.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task LoadRoutineItemsAsync()
    {
        try
        {
            _logger.LogInformation("Loading routine items for {Day}", SelectedDay);
            List<RoutineItem> items = await _routineService.GetRoutineForDay(SelectedDay);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                RoutineItems.Clear();
                foreach (RoutineItem item in items)
                {
                    RoutineItems.Add(item);
                }
            });
            _logger.LogInformation("Loaded {Count} routine items", items.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load routine items for {Day}", SelectedDay);
            await Shell.Current.DisplayAlertAsync(
                "Error",
                $"Failed to load routine: {ex.Message}",
                "OK"
            );
        }
    }

    [RelayCommand]
    private async Task ViewProgress(RoutineItem item)
    {
        await Shell.Current.GoToAsync($"{nameof(ProgressPage)}?ExerciseName={item.ExerciseName}");
    }

    [RelayCommand]
    private async Task DeleteRoutineItemAsync(RoutineItem item)
    {
        if (item == null || IsBusy)
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
                    "Deleting routine item {Id} - {Name}",
                    item.Id,
                    item.ExerciseName
                );
                await _routineService.DeleteRoutineItemAndReorderAsync(item);
                await LoadRoutineItemsAsync();
                await Shell.Current.DisplayAlertAsync(
                    "Item Removed",
                    "The routine item had been deleted",
                    "OK"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete routine item {Id}", item.Id);
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
        _ = LoadRoutineItemsAsync();
    }

    [ObservableProperty]
    private string _newExerciseName = string.Empty;

    [ObservableProperty]
    private string _newOrder = string.Empty;

    [RelayCommand]
    private async Task SaveRoutineAsync()
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

            if (IsEditing && _editingItem != null)
            {
                _logger.LogInformation("Updating routine item {Id}", _editingItem.Id);
                oldDay = _editingItem.DayOfWeek;
                _editingItem.ExerciseName = NewExerciseName;
                _editingItem.Order = order;
                _editingItem.DayOfWeek = targetDay;
                await _routineService.ApplyRoutineChangesAsync(_editingItem, oldDay);
            }
            else
            {
                _logger.LogInformation("Adding new routine item: {Name}", NewExerciseName);
                RoutineItem item = new()
                {
                    ExerciseName = NewExerciseName,
                    Order = order,
                    DayOfWeek = targetDay,
                };
                await _routineService.ApplyRoutineChangesAsync(item);
            }

            await LoadRoutineItemsAsync();
            HideAddRoutine();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save routine item");
            await Shell.Current.DisplayAlertAsync("Database Error", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
