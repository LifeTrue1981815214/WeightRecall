using Microsoft.Extensions.Logging;
using Serilog;
using WeightRecall.Services;

namespace WeightRecall;

public partial class App : Application
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<App> _logger;

    public App(NotificationService notificationService, ILogger<App> logger)
    {
        InitializeComponent();
        _notificationService = notificationService;
        _logger = logger;

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            _logger.LogCritical(e.ExceptionObject as Exception, "Unhandled AppDomain exception");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _logger.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    protected override async void OnStart()
    {
        base.OnStart();
        try
        {
            _logger.LogInformation("App starting...");
            _ = await _notificationService.RequestNotificationPermission();
            await _notificationService.ScheduleDailyNotifications();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during App Start");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        _logger.LogInformation("Creating app window");
        return new Window(new AppShell(_notificationService));
    }
}
