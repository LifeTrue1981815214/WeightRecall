using System.Diagnostics;
using SQLite;
using WeightRecall.Models;

namespace WeightRecall.Data;

public class DatabaseContext
{
    private bool _isInitialized;

    public DatabaseContext()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3");

#if ANDROID
        // Use external files dir on Android to make the database file easier to access for debugging
        string? androidPath = Android
            .App.Application.Context.GetExternalFilesDir(null)
            ?.AbsolutePath;
        if (androidPath != null)
        {
            databasePath = Path.Combine(androidPath, "WeightRecall.db3");
        }
#endif

        Debug.WriteLine($"[DB] Database Path: {databasePath}");
        Connection = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _ = await Connection.CreateTableAsync<RoutineItem>();
        _ = await Connection.CreateTableAsync<WorkoutLog>();
        Debug.WriteLine("[DB] Tables initialized successfully.");

        await SeedData();
        _isInitialized = true;
    }

    public SQLiteAsyncConnection Connection { get; }

    private async Task SeedData()
    {
        // Only seed if RoutineItem is empty
        // if (await Connection.Table<RoutineItem>().CountAsync() == 0)
        // {
        //     Debug.WriteLine("[DB] Seeding dummy data...");

        //     _ = await Connection.InsertAsync(
        //         new RoutineItem
        //         {
        //             ExerciseName = "Bench Press",
        //             DayOfWeek = DayOfWeek.Monday,
        //             Order = 1,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new RoutineItem
        //         {
        //             ExerciseName = "Squats",
        //             DayOfWeek = DayOfWeek.Monday,
        //             Order = 2,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new RoutineItem
        //         {
        //             ExerciseName = "Pec Fly",
        //             DayOfWeek = DayOfWeek.Monday,
        //             Order = 3,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new RoutineItem
        //         {
        //             ExerciseName = "Deadlift",
        //             DayOfWeek = DayOfWeek.Tuesday,
        //             Order = 1,
        //         }
        //     );

        //     // Seed WorkoutLog
        //     _ = await Connection.InsertAsync(
        //         new WorkoutLog
        //         {
        //             Date = DateTime.Now.Date,
        //             ExerciseName = "Bench Press",
        //             Sets = 3,
        //             Reps = 10,
        //             Weight = 60.5,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new WorkoutLog
        //         {
        //             Date = DateTime.Now.Date,
        //             ExerciseName = "Squats",
        //             Sets = 3,
        //             Reps = 12,
        //             Weight = 80.0,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new WorkoutLog
        //         {
        //             Date = DateTime.Now.Date,
        //             ExerciseName = "Pec Fly",
        //             Sets = 3,
        //             Reps = 15,
        //             Weight = 40.0,
        //         }
        //     );

        //     _ = await Connection.InsertAsync(
        //         new WorkoutLog
        //         {
        //             Date = DateTime.Now.AddDays(-1).Date,
        //             ExerciseName = "Deadlift",
        //             Sets = 3,
        //             Reps = 8,
        //             Weight = 100.0,
        //         }
        //     );

        //     Debug.WriteLine("[DB] Dummy data seeded successfully.");
        // }
    }
}
