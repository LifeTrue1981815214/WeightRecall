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

    /// <inheritdoc />
    /// <remarks>
    /// A toast, which Android draws near the bottom of the screen over whatever is showing.
    /// Marshalled explicitly: the platform requires it, and that requirement belongs here rather
    /// than being something every caller has to remember.
    /// </remarks>
    public Task ShowBriefMessageAsync(string message, bool isError = false)
    {
        MainThread.BeginInvokeOnMainThread(() =>
            Android
                .Widget.Toast.MakeText(
                    Android.App.Application.Context,
                    message,
                    isError ? Android.Widget.ToastLength.Long : Android.Widget.ToastLength.Short
                )
                ?.Show()
        );

        return Task.CompletedTask;
    }
}
