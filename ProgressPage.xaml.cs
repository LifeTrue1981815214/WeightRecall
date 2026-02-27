using WeightRecall.ViewModels;

namespace WeightRecall;

/// <summary>
/// Page displaying progress charts for a selected exercise.
/// </summary>
public partial class ProgressPage : ContentPage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressPage"/> class.
    /// </summary>
    /// <param name="viewModel">The view model for this page.</param>
    public ProgressPage(ProgressViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
