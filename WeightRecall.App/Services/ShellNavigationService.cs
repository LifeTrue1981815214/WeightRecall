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
        // Escaped because the name travels in a query string; an ampersand or plus in it
        // would otherwise truncate what the page receives.
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
