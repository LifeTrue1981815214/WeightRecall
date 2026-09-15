using WeightRecall.Models;

namespace WeightRecall.Repository;

/// <summary>
/// Abstraction over persistence of planned exercises, so callers can be tested
/// without a real database.
/// </summary>
public interface IPlannedExerciseRepository
{
    /// <summary>
    /// Retrieves every planned exercise.
    /// </summary>
    /// <returns>A list of all <see cref="PlannedExercise"/> entries.</returns>
    Task<List<PlannedExercise>> GetPlannedExercisesAsync();

    /// <summary>
    /// Retrieves a single planned exercise by its identifier.
    /// </summary>
    /// <param name="id">The identifier to look up.</param>
    /// <returns>The matching <see cref="PlannedExercise"/>, or null if no row has that id.</returns>
    Task<PlannedExercise?> GetPlannedExerciseAsync(int id);

    /// <summary>
    /// Retrieves the planned exercises for a specific day of the week, ordered by position.
    /// </summary>
    /// <param name="day">The day of the week to retrieve exercises for.</param>
    /// <returns>A list of <see cref="PlannedExercise"/> for the specified day.</returns>
    Task<List<PlannedExercise>> GetPlannedExercisesForDayAsync(DayOfWeek day);

    /// <summary>
    /// Adds a new planned exercise.
    /// </summary>
    /// <param name="exercise">The planned exercise to add.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> AddPlannedExerciseAsync(PlannedExercise exercise);

    /// <summary>
    /// Deletes a planned exercise.
    /// </summary>
    /// <param name="exercise">The planned exercise to delete.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> DeletePlannedExerciseAsync(PlannedExercise exercise);

    /// <summary>
    /// Updates an existing planned exercise.
    /// </summary>
    /// <param name="exercise">The planned exercise to update.</param>
    /// <returns>The number of rows affected.</returns>
    Task<int> UpdatePlannedExerciseAsync(PlannedExercise exercise);
}
