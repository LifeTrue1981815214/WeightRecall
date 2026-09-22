namespace WeightRecall.Abstractions;

/// <summary>
/// Moves the user between screens.
/// </summary>
/// <remarks>
/// Named by destination rather than by route, so route strings and their escaping stay in the app.
/// </remarks>
public interface INavigationService
{
    /// <summary>
    /// Opens the progress chart for a single exercise.
    /// </summary>
    Task GoToExerciseProgressAsync(string exerciseName);

    /// <summary>
    /// Returns to the previous screen.
    /// </summary>
    Task GoBackAsync();
}
