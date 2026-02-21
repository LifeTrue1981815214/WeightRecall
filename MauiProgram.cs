using Microsoft.Extensions.Logging;
using WeightRecall.Data;
using WeightRecall.Repository;
using WeightRecall.Services;
using WeightRecall.ViewModels;

namespace WeightRecall
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // Services and Data
            builder.Services.AddSingleton<DatabaseContext>();
            builder.Services.AddSingleton<RoutineRepository>();
            builder.Services.AddSingleton<RoutineService>();

            // ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<ExercisesViewModel>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ExercisesPage>();

            return builder.Build();
        }
    }
}
