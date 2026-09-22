namespace WeightRecall.Abstractions;

/// <summary>
/// Moves the user between screens.
/// </summary>
/// <remarks>
/// Named after destinations rather than routes on purpose. A view model decides <em>where the
/// user should end up</em>; how that is spelled as a route, and how the exercise name is encoded
/// into it, belongs to the app. Keeping the route strings on the other side of this interface
/// also means a typo in one is a compile error in one place rather than a dead link.
/// </remarks>
public interface INavigationService
{
    /// <summary>
    /// Opens the progress chart for a single exercise.
    /// </summary>
    /// <param name="exerciseName">The exercise whose history to show.</param>
    Task GoToExerciseProgressAsync(string exerciseName);

    /// <summary>
    /// Returns to the previous screen.
    /// </summary>
    Task GoBackAsync();
}
