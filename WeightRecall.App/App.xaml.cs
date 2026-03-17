using Microsoft.Extensions.Logging;
using Serilog;
using WeightRecall.Services;

namespace WeightRecall;

/// <summary>
/// Interaction logic for the main Application class.
/// Responsible for startup initialization, error handling, and window creation.
/// </summary>
public partial class App : Application
{
    private readonly NotificationService _notificationService;
    private readonly ILogger<App> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// Sets up global exception handling.
    /// </summary>
    /// <param name="notificationService">Service for managing notifications.</param>
    /// <param name="logger">Logger instance.</param>
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

    /// <summary>
    /// Triggered when the application starts.
    /// Requests notification permissions and schedules daily reminders if enabled.
    /// </summary>
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

    /// <summary>
    /// Creates the main window for the application.
    /// </summary>
    /// <param name="activationState">The activation state.</param>
    /// <returns>A new <see cref="Window"/>.</returns>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        _logger.LogInformation("Creating app window");
        return new Window(new AppShell(_notificationService));
    }
}
