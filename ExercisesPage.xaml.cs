using WeightRecall.ViewModels;

namespace WeightRecall
{
    public partial class ExercisesPage : ContentPage
    {
        public ExercisesPage(ExercisesViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
