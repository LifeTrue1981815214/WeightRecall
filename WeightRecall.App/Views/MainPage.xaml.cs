using WeightRecall.ViewModels;

namespace WeightRecall.Views;

/// <summary>
/// The main landing page of the application where users log their daily exercise progress.
/// </summary>
public partial class MainPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this page.</param>
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Triggered when the page appears on screen. Refresh the daily exercise list.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MainViewModel viewModel)
        {
            _ = viewModel.LoadTodayExercises();
        }
    }

    /// <summary>
    /// Navigation helper to go to the Exercises page.
    /// </summary>
    private async void OnGoToExercisesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }

    /// <summary>
    /// Navigation helper to go to the Main page.
    /// </summary>
    private async void OnGoToWeightRecallClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
