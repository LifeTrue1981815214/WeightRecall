namespace WeightRecall.Abstractions;

/// <summary>
/// Shows the simple prompts a view model needs to ask the user something.
/// </summary>
/// <remarks>
/// Exists so view models do not reach for the UI framework directly. Asking "did the user
/// confirm?" is a decision the view model makes, but putting up the box is not its job, and a
/// direct call would tie it to a running app -- there would be no way to exercise the branch
/// where the user cancels.
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Tells the user something. There is nothing to decide; the only button dismisses it.
    /// </summary>
    /// <param name="title">Heading of the message.</param>
    /// <param name="message">The body of the message.</param>
    /// <param name="dismiss">Label of the dismiss button.</param>
    Task AlertAsync(string title, string message, string dismiss = "OK");

    /// <summary>
    /// Asks the user to confirm or cancel.
    /// </summary>
    /// <param name="title">Heading of the question.</param>
    /// <param name="message">The body of the question.</param>
    /// <param name="accept">Label of the confirming button.</param>
    /// <param name="cancel">Label of the cancelling button.</param>
    /// <returns><c>true</c> if the user accepted, <c>false</c> if they cancelled.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
