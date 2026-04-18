using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Serilog;
using Serilog.Events;
using WeightRecall.Data;
using WeightRecall.Repository;
using WeightRecall.Services;
using WeightRecall.ViewModels;
using WeightRecall.Views;

namespace WeightRecall;

/// <summary>
/// Static class for configuring and building the MAUI application.
/// </summary>
public static class MauiProgram
{
    /// <summary>
    /// Configures the MAUI app, including logging, third-party libraries, and dependency injection.
    /// </summary>
    /// <returns>The configured <see cref="MauiApp"/>.</returns>
    public static MauiApp CreateMauiApp()
    {
        string logBaseDir = FileSystem.AppDataDirectory;

        Android.Content.Context context = Android.App.Application.Context;
        Java.IO.File? externalDir = context.GetExternalFilesDir(null);
        if (externalDir != null)
        {
            logBaseDir = externalDir.AbsolutePath;
        }

        string logPath = Path.Combine(logBaseDir, "logs", "log.txt");

        // Configure Serilog for file logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.Services.AddSingleton<IChartService, ChartService>();
        builder
            .UseMauiApp<App>()
            .UseMicrocharts()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Logging.AddSerilog(dispose: true);

#if DEBUG
        builder.Logging.AddDebug();
#endif
        // Register Services and Repositories for Dependency Injection
        builder.Services.AddSingleton<DatabaseContext>(sp => new DatabaseContext(
            Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3"),
            sp.GetRequiredService<ILogger<DatabaseContext>>()
        ));
        builder.Services.AddSingleton<RoutineRepository>();
        builder.Services.AddSingleton<RoutineService>();
        builder.Services.AddSingleton<WorkoutLogRepository>();
        builder.Services.AddSingleton<WorkoutLogService>();
        builder.Services.AddSingleton<NotificationService>();
        builder.Services.AddSingleton<IWorkoutNotificationService>(sp =>
            sp.GetRequiredService<NotificationService>()
        );
        builder.Services.AddSingleton<DateService>();

        // Register Shell and Settings
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<AppShell>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<ExercisesViewModel>();
        builder.Services.AddTransient<ProgressViewModel>();

        // Register Pages
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<ExercisesPage>();
        builder.Services.AddTransient<ProgressPage>();

        return builder.Build();
    }
}
