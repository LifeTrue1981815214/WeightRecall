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
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// Sets up global exception handling.
    /// </summary>
    /// <param name="notificationService">Service for managing notifications.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="serviceProvider">The DI service provider.</param>
    public App(
        NotificationService notificationService,
        ILogger<App> logger,
        IServiceProvider serviceProvider
    )
    {
        InitializeComponent();
        _notificationService = notificationService;
        _logger = logger;
        _serviceProvider = serviceProvider;

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
    /// Triggered when the application starts. Sets reminders up on first run, then schedules them.
    /// </summary>
    /// <remarks>
    /// The setup asks for notifications and, once allowed, sends the user to the exact-alarm
    /// screen if that is still needed. It runs ONCE, and the flag is written before the request
    /// rather than after: repeating it every launch would eject anyone who has not granted exact
    /// alarms straight back into system settings, with no way to reach the app. After first run
    /// the only way to be asked again is the settings toggle.
    /// </remarks>
    protected override async void OnStart()
    {
        base.OnStart();
        try
        {
            _logger.LogInformation("App starting...");

            if (!Preferences.Default.Get(NotificationService.RemindersRequestedKey, false))
            {
                Preferences.Default.Set(NotificationService.RemindersRequestedKey, true);
                _logger.LogInformation("First run: asking for reminder permissions");
                _ = await NotificationService.RequestReminderPermissions();
            }

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
        return new Window(_serviceProvider.GetRequiredService<AppShell>());
    }
}
