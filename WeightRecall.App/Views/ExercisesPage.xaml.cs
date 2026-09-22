using WeightRecall.ViewModels;

namespace WeightRecall.Views;

/// <summary>
/// Page for managing the weekly workout routine.
/// </summary>
public partial class ExercisesPage : ContentPage
{
    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>
    /// Refreshes the routine list. The only place the first load starts from; the view model
    /// must not start one of its own, or the two race.
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ExercisesViewModel viewModel)
        {
            _ = viewModel.LoadPlannedExercisesAsync();
        }
    }

    private async void OnGoToWeightRecallClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnGoToExercisesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }
}
