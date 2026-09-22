using WeightRecall.ViewModels;

namespace WeightRecall.Views;

/// <summary>
/// The main landing page of the application where users log their daily exercise progress.
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Refreshes the daily exercise list. The only place the first load starts from; the view
    /// model must not start one of its own, or the two race.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MainViewModel viewModel)
        {
            _ = viewModel.LoadTodayExercises();
        }
    }

    private async void OnGoToExercisesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }

    private async void OnGoToWeightRecallClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
