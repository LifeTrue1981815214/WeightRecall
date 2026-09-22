using WeightRecall.Models;

namespace WeightRecall.Domain;

/// <summary>
/// Presents one planned exercise as a row on the day's log sheet.
/// </summary>
public static class DailyExerciseLogBuilder
{
    /// <summary>
    /// Text shown when an exercise has never been logged before.
    /// </summary>
    public const string NoPreviousData = "No data from last week";

    /// <summary>
    /// Produces the row for a planned exercise: the log already recorded for that day if there is
    /// one, otherwise an empty placeholder to type into. Either way it carries a description of
    /// the last time the exercise was done.
    /// </summary>
    /// <returns>
    /// The row to display. When a log already exists this is that same instance, not a copy --
    /// replacing it would discard values the user has typed.
    /// </returns>
    public static ExerciseLog Build(
        PlannedExercise planned,
        ExerciseLog? existingLog,
        ExerciseLog? previousLog,
        DateTime date
    )
    {
        string previousDescription = DescribePrevious(previousLog);

        if (existingLog is not null)
        {
            existingLog.PreviousDescription = previousDescription;
            return existingLog;
        }

        return new ExerciseLog
        {
            Date = date.Date,
            ExerciseName = planned.ExerciseName,
            Weight = 0,
            Sets = 0,
            Reps = 0,
            PreviousDescription = previousDescription,
        };
    }

    /// <summary>
    /// Finds the log already recorded for a planned exercise on the day being shown.
    /// </summary>
    /// <remarks>
    /// Matched by name, case-insensitively: that is the only link between a planned exercise
    /// and its logs.
    /// </remarks>
    public static ExerciseLog? FindExistingLog(
        IEnumerable<ExerciseLog> existingLogs,
        PlannedExercise planned
    )
    {
        return existingLogs.FirstOrDefault(l =>
            l.ExerciseName.Equals(planned.ExerciseName, StringComparison.OrdinalIgnoreCase)
        );
    }

    /// <summary>
    /// Describes a previous performance for display above the day's inputs.
    /// </summary>
    /// <returns>A short summary, or <see cref="NoPreviousData"/> when there is nothing.</returns>
    public static string DescribePrevious(ExerciseLog? previousLog)
    {
        return previousLog is null
            ? NoPreviousData
            : $"Prev: {previousLog.Weight}kg | {previousLog.Sets} sets | {previousLog.Reps} reps";
    }
}
