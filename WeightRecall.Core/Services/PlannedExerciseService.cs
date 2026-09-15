using Microsoft.Extensions.Logging;
using WeightRecall.Models;
using WeightRecall.Repository;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing business logic related to planned exercises.
/// </summary>
/// <remarks>
/// Every public method that changes the routine leaves it in a consistent state and reschedules
/// notifications exactly once before returning. The private helpers those methods are built from
/// deliberately do neither, so they can be composed without redundant work.
/// </remarks>
/// <param name="repository">The planned exercise repository.</param>
/// <param name="workoutLogRepository">The workout log repository, so renames carry their history.</param>
/// <param name="notificationService">The notification service to sync reminders.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class PlannedExerciseService(
    IPlannedExerciseRepository repository,
    IWorkoutLogRepository workoutLogRepository,
    IWorkoutNotificationService notificationService,
    ILogger<PlannedExerciseService> logger
)
{
    private readonly IPlannedExerciseRepository _repository = repository;
    private readonly IWorkoutLogRepository _workoutLogRepository = workoutLogRepository;
    private readonly IWorkoutNotificationService _notificationService = notificationService;
    private readonly ILogger<PlannedExerciseService> _logger = logger;

    /// <summary>
    /// Retrieves the exercises planned for a specific day.
    /// </summary>
    /// <param name="day">Day of the week.</param>
    /// <returns>A list of <see cref="PlannedExercise"/>.</returns>
    public async Task<List<PlannedExercise>> GetPlannedExercisesForDayAsync(DayOfWeek day)
    {
        _logger.LogDebug("Retrieving planned exercises for {Day}", day);
        return await _repository.GetPlannedExercisesForDayAsync(day);
    }

    /// <summary>
    /// Adds a new planned exercise and updates the notifications.
    /// </summary>
    /// <param name="exercise">The exercise to add.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    public async Task<int> AddPlannedExerciseAsync(PlannedExercise exercise)
    {
        int result = await InsertAsync(exercise);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Updates an existing planned exercise and updates notifications.
    /// </summary>
    /// <param name="exercise">The exercise to update.</param>
    /// <returns>Number of rows affected.</returns>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    public async Task<int> UpdatePlannedExerciseAsync(PlannedExercise exercise)
    {
        int result = await ModifyAsync(exercise);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Deletes a planned exercise, closes the gap it leaves in that day's ordering,
    /// and updates notifications.
    /// </summary>
    /// <param name="exercise">The exercise to delete.</param>
    /// <returns>Number of rows deleted.</returns>
    public async Task<int> DeletePlannedExerciseAsync(PlannedExercise exercise)
    {
        _logger.LogInformation(
            "Deleting planned exercise: {Exercise} (Id: {Id})",
            exercise.ExerciseName,
            exercise.Id
        );

        int result = await _repository.DeletePlannedExerciseAsync(exercise);
        await ResequenceAsync(exercise.DayOfWeek);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Renumbers a day's exercises sequentially and updates notifications.
    /// </summary>
    /// <param name="day">Day of the week to reorder.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ReorderPlannedExercisesAsync(DayOfWeek day)
    {
        await ResequenceAsync(day);
        await _notificationService.ScheduleDailyNotifications();
    }

    /// <summary>
    /// Saves a planned exercise — inserting it when new, updating it otherwise — then renumbers
    /// every day it affects and updates notifications.
    /// </summary>
    /// <remarks>
    /// The saved exercise is given priority while its day is renumbered, so the position the
    /// user typed is the position it ends up in, and any exercise already sitting there is
    /// pushed down rather than keeping the spot.
    /// </remarks>
    /// <param name="exercise">The planned exercise to save.</param>
    /// <param name="oldDay">Optional previous day if the exercise was moved between days.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    public async Task SavePlannedExerciseAsync(PlannedExercise exercise, DayOfWeek? oldDay = null)
    {
        _ = exercise.Id == 0 ? await InsertAsync(exercise) : await ModifyAsync(exercise);

        await ResequenceAsync(exercise.DayOfWeek, exercise);

        if (oldDay.HasValue && oldDay.Value != exercise.DayOfWeek)
        {
            await ResequenceAsync(oldDay.Value);
        }

        await _notificationService.ScheduleDailyNotifications();
    }

    /// <summary>
    /// Validates and inserts an exercise, without touching notifications.
    /// </summary>
    /// <param name="exercise">The exercise to insert.</param>
    /// <returns>Number of rows affected.</returns>
    private async Task<int> InsertAsync(PlannedExercise exercise)
    {
        ValidateExerciseName(exercise);
        return await _repository.AddPlannedExerciseAsync(exercise);
    }

    /// <summary>
    /// Validates and updates an exercise, carrying its logged history along if it was renamed,
    /// and without touching notifications.
    /// </summary>
    /// <param name="exercise">The exercise to update.</param>
    /// <returns>Number of rows affected.</returns>
    private async Task<int> ModifyAsync(PlannedExercise exercise)
    {
        ValidateExerciseName(exercise);

        // Read the stored name before overwriting it -- the caller hands us an already-edited
        // instance, so this is the only place the previous name is still available.
        PlannedExercise? stored = await _repository.GetPlannedExerciseAsync(exercise.Id);
        string? previousName = stored?.ExerciseName;

        int result = await _repository.UpdatePlannedExerciseAsync(exercise);

        if (
            previousName is not null
            && !string.Equals(previousName, exercise.ExerciseName, StringComparison.Ordinal)
        )
        {
            await CarryHistoryToNewNameAsync(previousName, exercise);
        }

        return result;
    }

    /// <summary>
    /// Moves logged history from an exercise's previous name onto its new one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Logs are associated with an exercise by name rather than by id, which is deliberate: the
    /// same movement planned on two different days is two rows but one training history. The
    /// cost is that a rename would otherwise strand every past log under the old name, so the
    /// rename has to be followed through to the logs.
    /// </para>
    /// <para>
    /// Skipped when another planned exercise still carries the previous name, since that history
    /// belongs to it too and moving it would take the logs away from an exercise that is still
    /// using them.
    /// </para>
    /// </remarks>
    /// <param name="previousName">The name the exercise was stored under.</param>
    /// <param name="renamed">The exercise as it is now named.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CarryHistoryToNewNameAsync(string previousName, PlannedExercise renamed)
    {
        List<PlannedExercise> planned = await _repository.GetPlannedExercisesAsync();
        bool previousNameStillPlanned = planned.Any(e =>
            e.Id != renamed.Id
            && string.Equals(e.ExerciseName, previousName, StringComparison.Ordinal)
        );

        if (previousNameStillPlanned)
        {
            _logger.LogInformation(
                "Keeping history under {Previous}: another planned exercise still uses that name",
                previousName
            );
            return;
        }

        _ = await _workoutLogRepository.RenameExerciseAsync(previousName, renamed.ExerciseName);
    }

    /// <summary>
    /// Renumbers a day's exercises sequentially, persisting only the ones that moved and
    /// without touching notifications.
    /// </summary>
    /// <param name="day">Day of the week to renumber.</param>
    /// <param name="preferred">The exercise that should win a contested position, if any.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ResequenceAsync(DayOfWeek day, PlannedExercise? preferred = null)
    {
        List<PlannedExercise> exercises = await _repository.GetPlannedExercisesForDayAsync(day);
        List<PlannedExercise> moved = PlannedExerciseOrdering.AssignSequentialOrder(
            exercises,
            preferred
        );

        _logger.LogDebug("Resequencing {Day} moved {Count} exercise(s)", day, moved.Count);

        foreach (PlannedExercise exercise in moved)
        {
            _ = await _repository.UpdatePlannedExerciseAsync(exercise);
        }
    }

    /// <summary>
    /// Ensures an exercise carries a usable name.
    /// </summary>
    /// <param name="exercise">The exercise to validate.</param>
    /// <exception cref="ArgumentException">Thrown when exercise name is empty.</exception>
    private static void ValidateExerciseName(PlannedExercise exercise)
    {
        if (string.IsNullOrWhiteSpace(exercise.ExerciseName))
        {
            throw new ArgumentException("Exercise name is required.", nameof(exercise));
        }
    }
}
