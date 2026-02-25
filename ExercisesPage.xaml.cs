using WeightRecall.ViewModels;

namespace WeightRecall;

public partial class ExercisesPage : ContentPage
{
    public ExercisesPage(ExercisesViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnGoToWeightRecallClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnGoToExercisesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }
}
