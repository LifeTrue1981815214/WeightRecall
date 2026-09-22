using WeightRecall.Abstractions;
using WeightRecall.Views;

namespace WeightRecall.Services;

/// <summary>
/// Navigates through the app's shell.
/// </summary>
/// <remarks>
/// This is the only place that knows how a destination is spelled as a route.
/// </remarks>
public class ShellNavigationService : INavigationService
{
    /// <inheritdoc />
    public Task GoToExerciseProgressAsync(string exerciseName)
    {
        // The name travels in the query string, so anything in it that has a meaning there --
        // an ampersand, a space, a plus -- has to be escaped or the page receives a truncated name.
        return Shell.Current.GoToAsync(
            $"{nameof(ProgressPage)}?ExerciseName={Uri.EscapeDataString(exerciseName)}"
        );
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        return Shell.Current.GoToAsync("..");
    }
}
