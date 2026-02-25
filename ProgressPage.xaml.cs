using WeightRecall.ViewModels;

namespace WeightRecall;

public partial class ProgressPage : ContentPage
{
    public ProgressPage(ProgressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
