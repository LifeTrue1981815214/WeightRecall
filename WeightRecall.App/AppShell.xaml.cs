using WeightRecall.Views;

namespace WeightRecall;

/// <summary>
/// Interaction logic for the application shell, managing navigation and settings common to all pages.
/// </summary>
public partial class AppShell : Shell
{
    private readonly SettingsPage _settingsPage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// Configures routing for shell navigation.
    /// </summary>
    /// <param name="settingsPage">The settings page to present as a modal.</param>
    public AppShell(SettingsPage settingsPage)
    {
        InitializeComponent();
        _settingsPage = settingsPage;

        Routing.RegisterRoute(nameof(ProgressPage), typeof(ProgressPage));
    }

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(_settingsPage);
    }
}
