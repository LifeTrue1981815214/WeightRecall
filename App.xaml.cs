using WeightRecall.Services;

namespace WeightRecall;

public partial class App : Application
{
    private readonly NotificationService _notificationService;

    public App(NotificationService notificationService)
    {
        InitializeComponent();
        _notificationService = notificationService;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        await _notificationService.RequestNotificationPermission();
        await _notificationService.ScheduleDailyNotifications();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell(_notificationService));
    }
}
