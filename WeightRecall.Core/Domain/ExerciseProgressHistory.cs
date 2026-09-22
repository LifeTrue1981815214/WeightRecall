using WeightRecall.Models;

namespace WeightRecall.Domain;

/// <summary>
/// Aggregation rules for turning raw exercise logs into chart points, kept free of database and
/// clock concerns so they can be exercised in isolation.
/// </summary>
public static class ExerciseProgressHistory
{
    /// <summary>
    /// Reduces logs to one point per day, carrying that day's heaviest lift.
    /// </summary>
    /// <remarks>
    /// An exercise can be logged more than once on the same date -- separate sets recorded
    /// separately, or a correction. Those collapse into a single point rather than several
    /// points sharing an x-value, and the heaviest lift is what represents the day.
    /// </remarks>
    /// <param name="logs">The logs to summarize, in any order.</param>
    /// <returns>One <see cref="ExerciseProgressPoint"/> per day, oldest first.</returns>
    public static List<ExerciseProgressPoint> Summarize(IEnumerable<ExerciseLog> logs)
    {
        return
        [
            .. logs.GroupBy(l => l.Date.Date)
                .Select(g => new ExerciseProgressPoint(g.Key, g.Max(l => l.Weight)))
                .OrderBy(p => p.Date),
        ];
    }
}
