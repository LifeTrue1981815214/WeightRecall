using WeightRecall.Models;

namespace WeightRecall.Services;

/// <summary>
/// Rules for presenting one planned exercise as a row on the day's log sheet, kept free of
/// database concerns so they can be exercised in isolation.
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
    /// <param name="planned">The exercise planned for the day.</param>
    /// <param name="existingLog">A log already saved for this exercise on this date, if any.</param>
    /// <param name="previousLog">The most recent log before this date, if any.</param>
    /// <param name="date">The date being shown.</param>
    /// <returns>The row to display, which may be the existing log with its description updated.</returns>
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
    /// Matched by name, case-insensitively, because that is the only link between a planned
    /// exercise and its logs.
    /// </remarks>
    /// <param name="existingLogs">Logs already saved for the date.</param>
    /// <param name="planned">The exercise planned for the day.</param>
    /// <returns>The matching log, or null if the exercise has not been logged yet that day.</returns>
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
    /// <param name="previousLog">The most recent log before the date being shown, if any.</param>
    /// <returns>A short summary, or <see cref="NoPreviousData"/> when there is nothing to show.</returns>
    public static string DescribePrevious(ExerciseLog? previousLog)
    {
        return previousLog is null
            ? NoPreviousData
            : $"Prev: {previousLog.Weight}kg | {previousLog.Sets} sets | {previousLog.Reps} reps";
    }
}
