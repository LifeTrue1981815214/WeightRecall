using Microsoft.Extensions.Logging;
using WeightRecall.Abstractions;
using WeightRecall.Domain;
using WeightRecall.Models;
using WeightRecall.Repositories;

namespace WeightRecall.Services;

/// <summary>
/// Service for managing business logic related to planned exercises.
/// </summary>
/// <remarks>
/// Every public method that changes the routine reschedules notifications exactly once before
/// returning. The private helpers do not, so they can be composed without redundant work.
/// </remarks>
/// <param name="repository">The planned exercise repository.</param>
/// <param name="exerciseLogRepository">The exercise log repository, so renames carry their history.</param>
/// <param name="notificationService">The notification service to sync reminders.</param>
/// <param name="logger">The logger instance for diagnostics.</param>
public class PlannedExerciseService(
    IPlannedExerciseRepository repository,
    IExerciseLogRepository exerciseLogRepository,
    IWorkoutNotificationService notificationService,
    ILogger<PlannedExerciseService> logger
)
{
    private readonly IPlannedExerciseRepository _repository = repository;
    private readonly IExerciseLogRepository _exerciseLogRepository = exerciseLogRepository;
    private readonly IWorkoutNotificationService _notificationService = notificationService;
    private readonly ILogger<PlannedExerciseService> _logger = logger;

    /// <summary>
    /// Retrieves the exercises planned for a specific day.
    /// </summary>
    public async Task<List<PlannedExercise>> GetPlannedExercisesForDayAsync(DayOfWeek day)
    {
        _logger.LogDebug("Retrieving planned exercises for {Day}", day);
        return await _repository.GetPlannedExercisesForDayAsync(day);
    }

    /// <summary>
    /// Adds a new planned exercise and updates the notifications.
    /// </summary>
    public async Task<int> AddPlannedExerciseAsync(PlannedExercise exercise)
    {
        int result = await InsertAsync(exercise);
        await _notificationService.ScheduleDailyNotifications();
        return result;
    }

    /// <summary>
    /// Updates an existing planned exercise and updates notifications.
    /// </summary>
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
    /// Passed through as <c>preferred</c> while its day is renumbered, so the typed position is
    /// the one it ends up in.
    /// </remarks>
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
    /// Normalizes and inserts an exercise, without touching notifications.
    /// </summary>
    private async Task<int> InsertAsync(PlannedExercise exercise)
    {
        NormalizeExerciseName(exercise);
        return await _repository.AddPlannedExerciseAsync(exercise);
    }

    /// <summary>
    /// Normalizes and updates an exercise, carrying its logged history if it was renamed, and
    /// without touching notifications.
    /// </summary>
    private async Task<int> ModifyAsync(PlannedExercise exercise)
    {
        NormalizeExerciseName(exercise);

        // The caller hands us an already-edited instance, so this is the last chance to read
        // the previous name.
        PlannedExercise? stored = await _repository.GetPlannedExerciseAsync(exercise.Id);
        string? previousName = stored?.ExerciseName;

        int result = await _repository.UpdatePlannedExerciseAsync(exercise);

        if (ExerciseRenamePolicy.IsRename(previousName, exercise.ExerciseName))
        {
            await CarryHistoryToNewNameAsync(previousName!, exercise);
        }

        return result;
    }

    /// <summary>
    /// Moves logged history from an exercise's previous name onto its new one.
    /// </summary>
    /// <remarks>
    /// <see cref="ExerciseRenamePolicy"/> decides whether it moves; this only carries it out.
    /// </remarks>
    private async Task CarryHistoryToNewNameAsync(string previousName, PlannedExercise renamed)
    {
        List<PlannedExercise> planned = await _repository.GetPlannedExercisesAsync();

        if (!ExerciseRenamePolicy.ShouldMoveHistory(planned, renamed, previousName))
        {
            _logger.LogInformation(
                "Keeping history under {Previous}: another planned exercise still uses that name",
                previousName
            );
            return;
        }

        _ = await _exerciseLogRepository.RenameExerciseAsync(previousName, renamed.ExerciseName);
    }

    /// <summary>
    /// Renumbers a day's exercises sequentially, persisting only the ones that moved and
    /// without touching notifications.
    /// </summary>
    /// <param name="day">Day of the week to renumber.</param>
    /// <param name="preferred">The exercise that should win a contested position, if any.</param>
    private async Task ResequenceAsync(DayOfWeek day, PlannedExercise? preferred = null)
    {
        List<PlannedExercise> exercises = await _repository.GetPlannedExercisesForDayAsync(day);
        List<PlannedExercise> moved = PlannedExerciseOrdering.AssignSequentialPositions(
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
    /// Trims an exercise's name and ensures what remains is usable.
    /// </summary>
    /// <remarks>
    /// Logs are matched by name, so a trailing space would file a workout under a name that
    /// looks identical but does not match. Runs before the rename check, so tidying a name is
    /// not mistaken for renaming it.
    /// </remarks>
    /// <exception cref="ArgumentException">The name is empty or only whitespace.</exception>
    private static void NormalizeExerciseName(PlannedExercise exercise)
    {
        if (string.IsNullOrWhiteSpace(exercise.ExerciseName))
        {
            throw new ArgumentException("Exercise name is required.", nameof(exercise));
        }

        exercise.ExerciseName = exercise.ExerciseName.Trim();
    }
}
