using WeightRecall.Models;

namespace WeightRecall.Services;

/// <summary>
/// Ordering rules for planned exercises, kept free of database and notification
/// concerns so they can be exercised in isolation.
/// </summary>
public static class PlannedExerciseOrdering
{
    /// <summary>
    /// Assigns sequential positions (1, 2, 3, ...) to the supplied exercises, ranked by their
    /// current order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When the user types an order number, <paramref name="preferred"/> names the exercise they
    /// typed it for, and that number is treated as the <em>destination</em> for it: the other
    /// exercises are ranked among themselves and the preferred one is then slotted in at the
    /// requested position, pushing whatever was there out of the way.
    /// </para>
    /// <para>
    /// Placing it explicitly — rather than letting it contend for the position and breaking the
    /// tie in its favour — is what makes the move work in both directions. Winning a tie only
    /// ever pulls an exercise <em>ahead</em> of the one it collides with, which silently fails
    /// for downward moves: asking to move from 2 to 3 would put it back at 2.
    /// </para>
    /// <para>
    /// Exercises without a typed position are ranked by their current order, ties broken
    /// alphabetically by name so the result stays stable and predictable.
    /// </para>
    /// <para>
    /// Exercises are renumbered in place. Only those whose position actually changed are
    /// returned, so callers can persist the smallest possible number of rows.
    /// </para>
    /// </remarks>
    /// <param name="exercises">The exercises belonging to a single day.</param>
    /// <param name="preferred">
    /// The exercise whose <see cref="PlannedExercise.Order"/> should be honoured as a destination,
    /// matched by <see cref="PlannedExercise.Id"/>. Out-of-range values are clamped to the ends of
    /// the list. Pass null when no exercise has been singled out.
    /// </param>
    /// <returns>The exercises whose <see cref="PlannedExercise.Order"/> was changed.</returns>
    public static List<PlannedExercise> AssignSequentialOrder(
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
                .OrderBy(e => e.Order)
                .ThenBy(e => e.ExerciseName),
        ];

        if (target is not null)
        {
            ranked.Insert(Math.Clamp(target.Order - 1, 0, ranked.Count), target);
        }

        List<PlannedExercise> changed = [];

        for (int i = 0; i < ranked.Count; i++)
        {
            int position = i + 1;
            if (ranked[i].Order != position)
            {
                ranked[i].Order = position;
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
