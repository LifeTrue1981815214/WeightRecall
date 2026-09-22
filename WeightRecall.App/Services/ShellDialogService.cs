namespace WeightRecall.Services;

/// <summary>
/// Shows prompts through the app's shell.
/// </summary>
/// <remarks>
/// Deliberately thin: every method is one call. Anything with a decision in it belongs in the
/// view model, on the testable side of <see cref="IDialogService"/>.
/// </remarks>
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
