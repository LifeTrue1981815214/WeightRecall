using WeightRecall.ViewModels;

namespace WeightRecall.Views;

/// <summary>
/// Page for managing the weekly workout routine.
/// </summary>
public partial class ExercisesPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExercisesPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this page.</param>
    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Triggered when the page appears on screen. Refresh the routine list.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ExercisesViewModel viewModel)
        {
            _ = viewModel.LoadRoutineItemsAsync();
        }
    }

    /// <summary>
    /// Navigation helper to go to the Main page.
    /// </summary>
    private async void OnGoToWeightRecallClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    /// <summary>
    /// Navigation helper to go to the Exercises page.
    /// </summary>
    private async void OnGoToExercisesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }
}
