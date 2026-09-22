using WeightRecall.Abstractions;

namespace WeightRecall.Services;

/// <summary>
/// Shows prompts through the app's shell.
/// </summary>
public class ShellDialogService : IDialogService
{
    /// <inheritdoc />
    public Task AlertAsync(string title, string message, string dismiss = "OK")
    {
        return Shell.Current.DisplayAlertAsync(title, message, dismiss);
    }

    /// <inheritdoc />
    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
    }
}
