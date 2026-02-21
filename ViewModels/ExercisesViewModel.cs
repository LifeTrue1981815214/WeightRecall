using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.ViewModels
{
    public partial class ExercisesViewModel : ObservableObject
    {
        private readonly RoutineService _routineService;

        public ExercisesViewModel(RoutineService routineService)
        {
            _routineService = routineService;
            RoutineItems = new ObservableCollection<RoutineItem>();
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        public bool IsNotBusy => !IsBusy;

        public ObservableCollection<RoutineItem> RoutineItems { get; }

        [ObservableProperty]
        private bool _isAddingRoutine;

        [RelayCommand]
        private void ShowAddRoutine() => IsAddingRoutine = true;

        [RelayCommand]
        private void HideAddRoutine()
        {
            IsAddingRoutine = false;
            // Clear inputs when hiding
            NewExerciseName = string.Empty;
            NewOrder = string.Empty;
        }

        [RelayCommand]
        private async Task GetRoutineAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                await LoadRoutineItemsAsync();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadRoutineItemsAsync()
        {
            // Load exercises for the selected day
            var items = await _routineService.GetRoutineForDay(SelectedDay);

            RoutineItems.Clear();
            foreach (var item in items)
            {
                RoutineItems.Add(item);
            }
        }

        [RelayCommand]
        private async Task DeleteRoutineItemAsync(RoutineItem item)
        {
            if (item == null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                // Delete from Database
                await _routineService.DeleteRoutineItem(item);

                // No need to reload everything if we just remove the item we already have
                RoutineItems.Remove(item);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public List<DayOfWeek> AvailableDays { get; } = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>().ToList();

        [ObservableProperty]
        private DayOfWeek _selectedDay = DayOfWeek.Monday;

        partial void OnSelectedDayChanged(DayOfWeek value)
        {
            _ = LoadRoutineItemsAsync();
        }

        [ObservableProperty]
        private string _newExerciseName = string.Empty;

        [ObservableProperty]
        private string _newOrder = string.Empty;

        [RelayCommand]
        private async Task AddRoutineAsync()
        {
            if (IsBusy || string.IsNullOrWhiteSpace(NewExerciseName) || !int.TryParse(NewOrder, out int order))
                return;

            try
            {
                IsBusy = true;
                var newItem = new RoutineItem
                {
                    ExerciseName = NewExerciseName,
                    DayOfWeek = SelectedDay,
                    Order = order
                };

                await _routineService.AddRoutineItem(newItem);

                // Refresh list if the new item matches the currently viewed day
                await LoadRoutineItemsAsync();

                // Clear inputs and hide popup
                HideAddRoutine();
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
