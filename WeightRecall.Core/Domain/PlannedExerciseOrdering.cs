using WeightRecall.Models;

namespace WeightRecall.Domain;

/// <summary>
/// Ordering rules for planned exercises.
/// </summary>
public static class PlannedExerciseOrdering
{
    /// <summary>
    /// Assigns sequential positions (1, 2, 3, ...) to the supplied exercises, ranked by their
    /// current order.
    /// </summary>
    /// <remarks>
    /// <paramref name="preferred"/>'s position is a DESTINATION, not a tie-break: the others are
    /// ranked among themselves and it is slotted in, pushing whatever sat there out of the way.
    /// A tie-break would only ever pull an exercise forward, so moving 2 to 3 would silently
    /// leave it at 2. Out-of-range values clamp to the ends; ties elsewhere break by name.
    /// Renumbers in place and returns only what changed, so callers write the fewest rows.
    /// </remarks>
    /// <param name="preferred">Matched by <see cref="PlannedExercise.Id"/>; null when none.</param>
    /// <returns>The exercises whose position was changed.</returns>
    public static List<PlannedExercise> AssignSequentialPositions(
        IEnumerable<PlannedExercise> exercises,
        PlannedExercise? preferred = null
    )
    {
        List<PlannedExercise> all = [.. exercises];
        PlannedExercise? target = preferred is null
            ? null
            : all.Find(e => IsPreferred(e, preferred));

        List<PlannedExercise> ranked =
        [
            .. all.Where(e => !ReferenceEquals(e, target))
                .OrderBy(e => e.Position)
                .ThenBy(e => e.ExerciseName),
        ];

        if (target is not null)
        {
            ranked.Insert(Math.Clamp(target.Position - 1, 0, ranked.Count), target);
        }

        List<PlannedExercise> changed = [];

        for (int i = 0; i < ranked.Count; i++)
        {
            int position = i + 1;
            if (ranked[i].Position != position)
            {
                ranked[i].Position = position;
                changed.Add(ranked[i]);
            }
        }

        return changed;
    }

    /// <summary>
    /// Determines whether an exercise is the one whose typed position should be honoured.
    /// </summary>
    /// <remarks>
    /// Identity is compared by <see cref="PlannedExercise.Id"/> because the exercises being
    /// ranked are freshly loaded from the database and are therefore different instances from
    /// the one the caller edited. Reference equality is checked first so a not-yet-inserted
    /// exercise, whose Id is still 0, can be matched too.
    /// </remarks>
    /// <param name="candidate">The exercise being ranked.</param>
    /// <param name="preferred">The exercise whose typed position is being honoured, if any.</param>
    /// <returns>True when the candidate is the preferred exercise.</returns>
    private static bool IsPreferred(PlannedExercise candidate, PlannedExercise? preferred)
    {
        if (preferred is null)
        {
            return false;
        }

        return ReferenceEquals(candidate, preferred)
            || (candidate.Id != 0 && candidate.Id == preferred.Id);
    }
}
