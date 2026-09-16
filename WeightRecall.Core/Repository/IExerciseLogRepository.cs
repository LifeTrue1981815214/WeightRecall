using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// Abstraction over persistence of workout logs, so callers can be tested
/// without a real database.
/// </summary>
public interface IWorkoutLogRepository
{
    /// <summary>
    /// Retrieves all workout logs.
    /// </summary>
    /// <returns>A list of all <see cref="WorkoutLog"/> entries.</returns>
    Task<List<WorkoutLog>> GetWorkoutLogsAsync();

    /// <summary>
    /// Retrieves workout logs for a specific date.
    /// </summary>
    /// <param name="date">The date to retrieve logs for.</param>
    /// <returns>A list of <see cref="WorkoutLog"/> entries for the specified date.</returns>
    Task<List<WorkoutLog>> GetWorkoutLogForDateAsync(DateTime date);

    /// <summary>
    /// Retrieves the most recent workout log for an exercise on or before the specified date.
    /// </summary>
    /// <param name="exerciseName">The exercise name.</param>
    /// <param name="beforeDate">The latest date to consider (inclusive).</param>
    /// <returns>The latest <see cref="WorkoutLog"/> or null if none found.</returns>
    Task<WorkoutLog?> GetLatestLogForExerciseAsync(string exerciseName, DateTime beforeDate);

    /// <summary>
    /// Saves a workout log entry (inserts if new, updates if existing).
    /// </summary>
    /// <param name="item">The workout log entry to save.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> SaveWorkoutLogAsync(WorkoutLog item);

    /// <summary>
    /// Deletes a workout log entry.
    /// </summary>
    /// <param name="item">The workout log entry to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeleteWorkoutLogAsync(WorkoutLog item);

    /// <summary>
    /// Retrieves logs for a specific exercise within a given date range.
    /// </summary>
    /// <param name="exerciseName">The name of the exercise.</param>
    /// <param name="startDate">The start of the date range.</param>
    /// <param name="endDate">The end of the date range.</param>
    /// <returns>A list of matching <see cref="WorkoutLog"/> entries.</returns>
    Task<List<WorkoutLog>> GetLogsForExerciseInDateRangeAsync(
        string exerciseName,
        DateTime startDate,
        DateTime endDate
    );

    /// <summary>
    /// Repoints every log recorded under one exercise name to another name.
    /// </summary>
    /// <remarks>
    /// Logs are associated with an exercise by name, so a rename has to be followed through to
    /// them or the history is stranded under a name nothing refers to any more. See
    /// <c>PlannedExerciseService</c> for when this is and is not applied.
    /// </remarks>
    /// <param name="previousName">The name the logs are currently recorded under.</param>
    /// <param name="newName">The name to move them to.</param>
    /// <returns>The number of logs moved.</returns>
    Task<int> RenameExerciseAsync(string previousName, string newName);
}
