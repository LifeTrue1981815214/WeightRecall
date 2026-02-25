using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using WeightRecall.Data;
using WeightRecall.Repository;
using WeightRecall.Services;
using WeightRecall.ViewModels;

namespace WeightRecall;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMicrocharts()
            .UseLocalNotification()
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
        builder.Services.AddSingleton<WorkoutLogRepository>();
        builder.Services.AddSingleton<WorkoutLogService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<DateService>();

        // ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ExercisesViewModel>();
        builder.Services.AddTransient<ProgressViewModel>();

        // Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ExercisesPage>();
        builder.Services.AddTransient<ProgressPage>();

        return builder.Build();
    }
}
