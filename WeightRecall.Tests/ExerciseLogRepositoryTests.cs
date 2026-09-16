// ─────────────────────────────────────────────────────────────────────────────
// These usings bring in the types used in this file.
// xUnit (the test framework) picks up [Fact] / [Theory] automatically — no
// explicit "using Xunit;" needed because the project already references it
// via a global using in the .csproj.
// ─────────────────────────────────────────────────────────────────────────────
using Microsoft.Extensions.Logging.Abstractions; // NullLogger — a no-op logger so we don't need a real one in tests
using WeightRecall.Data; // DatabaseContext
using WeightRecall.Models; // ExerciseLog
using WeightRecall.Repository; // ExerciseLogRepository

namespace WeightRecall.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GROUP RELATED TESTS IN ONE CLASS.
// All tests for ExerciseLogRepository live here. Each public method on the
// repository should get its own test method (or several, one per scenario).
// ─────────────────────────────────────────────────────────────────────────────
public class ExerciseLogRepositoryTests
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
    public async Task SaveExerciseLogAsync_WithIdZero_InsertsLogAndGeneratesNewId()
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
        ExerciseLogRepository repository = new(context, NullLogger<ExerciseLogRepository>.Instance);

        // Build the object you will pass to the method under test.
        // Id = 0 tells the repository this is a NEW record (not an update).
        ExerciseLog newLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = DateTime.UtcNow,
        };

        // ── ACT ───────────────────────────────────────────────────────────────
        // "Act" means: call exactly the one method you are testing.
        // Capture the return value so you can assert on it below.
        int rowsAffected = await repository.SaveExerciseLogAsync(newLog);

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
        List<ExerciseLog> allLogs = await repository.GetExerciseLogsAsync();
        ExerciseLog? savedLog = allLogs.FirstOrDefault(l => l.Id == newLog.Id);

        // Assert.NotNull fails the test if savedLog is null, which would mean
        // the record was never persisted.
        Assert.NotNull(savedLog);
        Assert.Equal("Bench Press", savedLog.ExerciseName);
    }

    [Fact]
    public async Task SaveExerciseLogAsync_WithExistingId_UpdatesLog()
    {
        // ── ARRANGE ──────────────────────────────────────────────────────────
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        ExerciseLogRepository repository = new(context, NullLogger<ExerciseLogRepository>.Instance);

        // First insert a row so we have something to update.
        // After SaveExerciseLogAsync returns, newLog.Id will be set to the
        // auto-generated primary key — we use that Id in the update below.
        ExerciseLog existingLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified),
            Weight = 80,
        };
        _ = await context.Connection.InsertAsync(existingLog);

        // Modify the object. Because Id is now > 0, SaveExerciseLogAsync will
        // call UpdateAsync instead of InsertAsync.
        existingLog.Weight = 100;

        // ── ACT ───────────────────────────────────────────────────────────────
        int rowsAffected = await repository.SaveExerciseLogAsync(existingLog);

        // ── ASSERT ────────────────────────────────────────────────────────────
        Assert.Equal(1, rowsAffected);

        // Fetch the record back and confirm the weight was updated, not duplicated.
        List<ExerciseLog> allLogs = await repository.GetExerciseLogsAsync();
        ExerciseLog updatedLog = Assert.Single(allLogs); // fails if count != 1
        Assert.Equal(100, updatedLog.Weight);
    }

    [Fact]
    public async Task GetExerciseLogForDateAsync_WithExistingWorkoutOnADate_ReturnsThatExerciseLog()
    {
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        ExerciseLogRepository repository = new(context, NullLogger<ExerciseLogRepository>.Instance);

        DateTime dateNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        ExerciseLog existingLog = new()
        {
            Id = 0,
            ExerciseName = "Bench Press",
            Date = dateNow,
            Weight = 100,
        };
        _ = await context.Connection.InsertAsync(existingLog);
        List<ExerciseLog> result = await repository.GetExerciseLogForDateAsync(dateNow);

        ExerciseLog returnedLog = Assert.Single(result);
        Assert.Equal(existingLog.Id, returnedLog.Id);
        Assert.Equal(existingLog.ExerciseName, returnedLog.ExerciseName);
        Assert.Equal(existingLog.Date, returnedLog.Date);
        Assert.Equal(existingLog.Weight, returnedLog.Weight);
    }

    [Fact]
    public async Task GetLatestLogForExerciseAsync_WithExistingWorkoutOnADate_ReturnsTheMostRecentExerciseLogBeforeDate()
    {
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        ExerciseLogRepository repository = new(context, NullLogger<ExerciseLogRepository>.Instance);

        string exerciseName = "Bench Press";
        DateTime dateDayBefore = DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(-1),
            DateTimeKind.Unspecified
        );
        DateTime dateNow = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        ExerciseLog existingLog = new()
        {
            Id = 0,
            ExerciseName = exerciseName,
            Date = dateDayBefore,
            Weight = 100,
        };
        _ = await context.Connection.InsertAsync(existingLog);
        ExerciseLog? result = await repository.GetLatestLogForExerciseAsync(
            exerciseName,
            dateDayBefore
        );
        Assert.NotNull(result);
        Assert.Equal(existingLog.Id, result.Id);
        Assert.Equal(existingLog.ExerciseName, result.ExerciseName);
        Assert.Equal(existingLog.Date, result.Date);
        Assert.Equal(existingLog.Weight, result.Weight);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // REGRESSION GUARD.
    // RenameExerciseAsync is the only method that names the table in raw SQL, so
    // it is the only one the compiler cannot check. A rename of the C# type once
    // rewrote that string while the [Table] attribute stayed pinned, and every
    // rename threw "no such table" until this was caught. The table name now
    // comes from ExerciseLog.TableName; this test proves the query still runs.
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task RenameExerciseAsync_MovesLogsToTheNewName()
    {
        await using DatabaseContext context = await DatabaseContext.CreateForTestingAsync();
        ExerciseLogRepository repository = new(context, NullLogger<ExerciseLogRepository>.Instance);

        DateTime date = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Unspecified);
        _ = await repository.SaveExerciseLogAsync(
            new ExerciseLog
            {
                ExerciseName = "Pull ups",
                Date = date,
                Weight = 10,
            }
        );
        _ = await repository.SaveExerciseLogAsync(
            new ExerciseLog
            {
                ExerciseName = "Dips",
                Date = date,
                Weight = 12.5,
            }
        );

        int moved = await repository.RenameExerciseAsync("Pull ups", "Chin ups");

        // Only the matching logs move, and they are reachable under the new name.
        Assert.Equal(1, moved);
        List<ExerciseLog> all = await repository.GetExerciseLogsAsync();
        Assert.Contains(all, l => l.ExerciseName == "Chin ups");
        Assert.Contains(all, l => l.ExerciseName == "Dips");
        Assert.DoesNotContain(all, l => l.ExerciseName == "Pull ups");
    }
}
