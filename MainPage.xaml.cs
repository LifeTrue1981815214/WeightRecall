using WeightRecall.ViewModels;

namespace WeightRecall;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private async void OnGoToExercisesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ExercisesPage");
    }

    private async void OnGoToWeightRecallClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
