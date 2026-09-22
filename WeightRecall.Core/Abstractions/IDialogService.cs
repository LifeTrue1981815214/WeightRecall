namespace WeightRecall.Abstractions;

/// <summary>
/// Shows the prompts a view model needs to ask the user something.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Tells the user something. The only button dismisses it.
    /// </summary>
    Task AlertAsync(string title, string message, string dismiss = "OK");

    /// <summary>
    /// Asks the user to confirm or cancel.
    /// </summary>
    /// <returns><c>true</c> if the user accepted.</returns>
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}
