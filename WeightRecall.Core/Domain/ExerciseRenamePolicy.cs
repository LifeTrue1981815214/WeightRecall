using WeightRecall.Models;

namespace WeightRecall.Domain;

/// <summary>
/// Decides whether an exercise was renamed, and whether its logged history should follow.
/// </summary>
public static class ExerciseRenamePolicy
{
    /// <summary>
    /// Determines whether an edit actually changed the exercise's name.
    /// </summary>
    /// <remarks>
    /// Ordinal on purpose: a casing-only change IS a rename, because logs are matched with SQL
    /// <c>=</c>, which is case-sensitive. Treating "dips" and "Dips" as equal strands the history.
    /// </remarks>
    public static bool IsRename(string? previousName, string currentName)
    {
        return previousName is not null
            && !string.Equals(previousName, currentName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether logged history recorded under the previous name should move to the new one.
    /// </summary>
    /// <remarks>
    /// Logs are keyed by name, not id, so the same movement planned on two days shares one
    /// history. It therefore only moves when nothing else still answers to the old name --
    /// otherwise renaming the Monday entry would take the logs from the Thursday one.
    /// </remarks>
    public static bool ShouldMoveHistory(
        IEnumerable<PlannedExercise> allPlanned,
        PlannedExercise renamed,
        string previousName
    )
    {
        return !allPlanned.Any(e =>
            e.Id != renamed.Id
            && string.Equals(e.ExerciseName, previousName, StringComparison.Ordinal)
        );
    }
}
