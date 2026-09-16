// ─────────────────────────────────────────────────────────────────────────────
// These usings bring in the types used in this file.
// xUnit (the test framework) picks up [Fact] / [Theory] automatically — no
// explicit "using Xunit;" needed because the project already references it
// via a global using in the .csproj.
// ─────────────────────────────────────────────────────────────────────────────
using Microsoft.Extensions.Logging.Abstractions; // NullLogger — a no-op logger so we don't need a real one in tests
using WeightRecall.Data; // DatabaseContext
using WeightRecall.Models; // WorkoutLog
using WeightRecall.Repository; // WorkoutLogRepository

namespace WeightRecall.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GROUP RELATED TESTS IN ONE CLASS.
// All tests for WorkoutLogRepository live here. Each public method on the
// repository should get its own test method (or several, one per scenario).
// ─────────────────────────────────────────────────────────────────────────────
public class WorkoutLogRepositoryTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // TEST METHOD NAMING CONVENTION:
    //   MethodUnderTest_Scenario_ExpectedOutcome
    //
    // [Fact] marks a test that always runs with the same inputs.
    // Use [Theory] + [InlineData(...)] when you want to run the same test
    // with multiple different inputs.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SaveWorkoutLogAsync_WithIdZero_InsertsLogAndGeneratesNewId()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        // "Arrange" means: set up everything the test needs before calling the
        // method you are testing.

        // CreateForTestingAsync() opens a SQLite :memory: database and creates all
        // tables — it exists only
        // for the lifetime of this test and is destroyed automatically when
        // DisposeAsync() is called at the closing brace (that's what
        // "await using" does). No temp files, no cleanup code needed.
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();

        // NullLogger.Instance is a logger that silently discards every message.
        // It satisfies the ILogger<T> parameter without requiring a real logger.
        // Use this pattern whenever the class under test needs a logger but the
        // log output is not what you are testing.
        WorkoutLogRepository repository = new(context, NullLogger<WorkoutLogRepository>.Instance);

        // Build the object you will pass to the method under test.
        // Id = 0 tells the repository this is a NEW record (not an update).
        WorkoutLog newLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = DateTime.UtcNow,
        };

        // ── ACT ───────────────────────────────────────────────────────────────
        // "Act" means: call exactly the one method you are testing.
        // Capture the return value so you can assert on it below.
        int rowsAffected = await repository.SaveWorkoutLogAsync(newLog);

        // ── ASSERT ────────────────────────────────────────────────────────────
        // "Assert" means: verify the outcome matches what you expected.
        // Each Assert call will fail the test with a clear message if the
        // condition is not met.

        // The method should report that 1 row was written to the database.
        Assert.Equal(1, rowsAffected);

        // sqlite-net mutates the object after insert and sets Id to the
        // auto-incremented primary key. A value > 0 confirms the insert worked.
        Assert.True(newLog.Id > 0, "The ID should be greater than 0 after insertion.");

        // Round-trip check: fetch all logs from the DB and confirm the record
        // is actually there with the correct data — not just that the method
        // returned the right number.
        List<WorkoutLog> allLogs = await repository.GetWorkoutLogsAsync();
        WorkoutLog? savedLog = allLogs.FirstOrDefault(l => l.Id == newLog.Id);

        // Assert.NotNull fails the test if savedLog is null, which would mean
        // the record was never persisted.
        Assert.NotNull(savedLog);
        Assert.Equal("Bench Press", savedLog.ExerciseName);
    }

    [Fact]
    public async Task SaveWorkoutLogAsync_WithExistingId_UpdatesLog()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        WorkoutLogRepository repository = new(context, NullLogger<WorkoutLogRepository>.Instance);

        // First insert a row so we have something to update.
        // After SaveWorkoutLogAsync returns, newLog.Id will be set to the
        // auto-generated primary key — we use that Id in the update below.
        WorkoutLog existingLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            Weight = 80,
        };
        _ = await context.Connection.InsertAsync(existingLog);

        // Modify the object. Because Id is now > 0, SaveWorkoutLogAsync will
        // call UpdateAsync instead of InsertAsync.
        existingLog.Weight = 100;

        // ── ACT ───────────────────────────────────────────────────────────────
        int rowsAffected = await repository.SaveWorkoutLogAsync(existingLog);

        // ── ASSERT ────────────────────────────────────────────────────────────
        Assert.Equal(1, rowsAffected);

        // Fetch the record back and confirm the weight was updated, not duplicated.
        List<WorkoutLog> allLogs = await repository.GetWorkoutLogsAsync();
        WorkoutLog updatedLog = Assert.Single(allLogs); // fails if count != 1
        Assert.Equal(100, updatedLog.Weight);
    }

    [Fact]
    public async Task GetWorkoutLogForDateAsync_WithExistingWorkoutOnADate_ReturnsThatWorkoutLog()
    {
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        WorkoutLogRepository repository = new(context, NullLogger<WorkoutLogRepository>.Instance);

        DateTime dateNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        WorkoutLog existingLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = dateNow,
            Weight = 100,
        };
        _ = await context.Connection.InsertAsync(existingLog);
        List<WorkoutLog> result = await repository.GetWorkoutLogForDateAsync(dateNow);

        WorkoutLog returnedLog = Assert.Single(result);
        Assert.Equal(existingLog.Id, returnedLog.Id);
        Assert.Equal(existingLog.ExerciseName, returnedLog.ExerciseName);
        Assert.Equal(existingLog.Date, returnedLog.Date);
        Assert.Equal(existingLog.Weight, returnedLog.Weight);
    }

    [Fact]
    public async Task GetLatestLogForExerciseAsync_WithExistingWorkoutOnADate_ReturnsTheMostRecentWorkoutLogBeforeDate()
    {
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        WorkoutLogRepository repository = new(context, NullLogger<WorkoutLogRepository>.Instance);

        string exerciseName = "Bench Press";
        DateTime dateDayBefore = DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(-1),
            DateTimeKind.Unspecified
        );
        DateTime dateNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        WorkoutLog existingLog = new()
        {
            Id = 0,
            ExerciseName = exerciseName,
            Date = dateDayBefore,
            Weight = 100,
        };
        _ = await context.Connection.InsertAsync(existingLog);
        WorkoutLog? result = await repository.GetLatestLogForExerciseAsync(
            exerciseName,
            dateDayBefore
        );
        Assert.NotNull(result);
        Assert.Equal(existingLog.Id, result.Id);
        Assert.Equal(existingLog.ExerciseName, result.ExerciseName);
        Assert.Equal(existingLog.Date, result.Date);
        Assert.Equal(existingLog.Weight, result.Weight);
    }
}
