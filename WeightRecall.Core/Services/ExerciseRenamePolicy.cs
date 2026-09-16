using WeightRecall.Models;

namespace WeightRecall.Services;

/// <summary>
/// Rules deciding whether a planned exercise was renamed and whether its logged history should
/// follow, kept free of database concerns so they can be exercised in isolation.
/// </summary>
public static class ExerciseRenamePolicy
{
    /// <summary>
    /// Determines whether an edit actually changed the exercise's name.
    /// </summary>
    /// <remarks>
    /// Compared with <see cref="StringComparison.Ordinal"/> on purpose. A change of casing alone
    /// is a real rename here, because logs are matched with SQL <c>=</c>, which is case-sensitive:
    /// treating "dips" and "Dips" as the same name would leave the history behind.
    /// </remarks>
    /// <param name="previousName">The name the exercise was stored under, or null if unknown.</param>
    /// <param name="currentName">The name the exercise now carries.</param>
    /// <returns>True when the name changed and the history needs considering.</returns>
    public static bool IsRename(string? previousName, string currentName)
    {
        return previousName is not null
            && !string.Equals(previousName, currentName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines whether logged history recorded under the previous name should move to the new one.
    /// </summary>
    /// <remarks>
    /// Logs are associated with an exercise by name rather than by id, which is deliberate: the
    /// same movement planned on two different days is two rows but one training history. So the
    /// history only moves when nothing else still answers to the old name -- otherwise renaming
    /// the Monday entry would take the logs away from the Thursday one that is still using them.
    /// </remarks>
    /// <param name="allPlanned">Every planned exercise, across all days.</param>
    /// <param name="renamed">The exercise that was renamed, excluded from the search by its id.</param>
    /// <param name="previousName">The name the exercise was stored under.</param>
    /// <returns>True when the previous name is now unused and its history should follow the rename.</returns>
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
