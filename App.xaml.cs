using Microsoft.Extensions.DependencyInjection;

namespace WeightRecall
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}

//using WeightRecall.Data;

//namespace WeightRecall
//{
//    public partial class App : Application
//    {
//        public App(DatabaseContext dbContext)
//        {
//            InitializeComponent();

//            MainPage = new AppShell();

//            // Trigger database creation and seeding
//            Task.Run(async () => await dbContext.GetConnectionAsync());
//        }
//    }
//}