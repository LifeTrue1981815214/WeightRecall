using WeightRecall.Models;

namespace WeightRecall.Repositories;

/// <summary>
/// Persistence of exercise logs.
/// </summary>
public interface IExerciseLogRepository
{
    /// <summary>
    /// Retrieves all exercise logs.
    /// </summary>
    /// <returns>A list of all <see cref="ExerciseLog"/> entries.</returns>
    Task<List<ExerciseLog>> GetExerciseLogsAsync();

    /// <summary>
    /// Retrieves exercise logs for a specific date.
    /// </summary>
    /// <param name="date">The date to retrieve logs for.</param>
    /// <returns>A list of <see cref="ExerciseLog"/> entries for the specified date.</returns>
    Task<List<ExerciseLog>> GetExerciseLogForDateAsync(DateTime date);

    /// <summary>
    /// Retrieves the most recent exercise log for an exercise on or before the specified date.
    /// </summary>
    /// <param name="exerciseName">The exercise name.</param>
    /// <param name="beforeDate">The latest date to consider (inclusive).</param>
    /// <returns>The latest <see cref="ExerciseLog"/> or null if none found.</returns>
    Task<ExerciseLog?> GetLatestLogForExerciseAsync(string exerciseName, DateTime beforeDate);

    /// <summary>
    /// Saves an exercise log entry (inserts if new, updates if existing).
    /// </summary>
    /// <param name="item">The exercise log entry to save.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveExerciseLogAsync(ExerciseLog item);

    /// <summary>
    /// Deletes an exercise log entry.
    /// </summary>
    /// <param name="item">The exercise log entry to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteExerciseLogAsync(ExerciseLog item);

    /// <summary>
    /// Retrieves logs for a specific exercise within a given date range.
    /// </summary>
    /// <param name="exerciseName">The name of the exercise.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <returns>A list of matching <see cref="ExerciseLog"/> entries.</returns>
    Task<List<ExerciseLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    );

    /// <summary>
    /// Repoints every log recorded under one exercise name to another name.
    /// </summary>
    /// <remarks>
    /// Logs are keyed by name, so a rename must be followed through or the history is stranded.
    /// <c>ExerciseRenamePolicy</c> decides when this applies.
    /// </remarks>
    /// <returns>The number of logs moved.</returns>
    Task<int> RenameExerciseAsync(string previousName, string newName);
}
