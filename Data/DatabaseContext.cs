using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SQLite;
using WeightRecall.Models;

namespace WeightRecall.Data
{
    public class DatabaseContext
    {
        private readonly SQLiteAsyncConnection _connection;
        private bool _isInitialized;

        public DatabaseContext()
        {
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3");

#if ANDROID
            // Use external files dir on Android to make the database file easier to access for debugging
            var androidPath = Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath;
            if (androidPath != null)
            {
                databasePath = Path.Combine(androidPath, "WeightRecall.db3");
            }
#endif

            Debug.WriteLine($"[DB] Database Path: {databasePath}");
            _connection = new SQLiteAsyncConnection(databasePath);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized)
                return;

            await _connection.CreateTableAsync<RoutineItem>();
            await _connection.CreateTableAsync<WorkoutLog>();
            Debug.WriteLine("[DB] Tables initialized successfully.");

            await SeedData();
            _isInitialized = true;
        }

        public SQLiteAsyncConnection Connection => _connection;

        private async Task SeedData()
        {
            // Only seed if RoutineItem is empty
            if (await _connection.Table<RoutineItem>().CountAsync() == 0)
            {
                Debug.WriteLine("[DB] Seeding dummy data...");

                await _connection.InsertAsync(new RoutineItem
                {
                    ExerciseName = "Bench Press",
                    DayOfWeek = DayOfWeek.Monday,
                    Order = 1
                });

                await _connection.InsertAsync(new RoutineItem
                {
                    ExerciseName = "Squat",
                    DayOfWeek = DayOfWeek.Monday,
                    Order = 2
                });

                await _connection.InsertAsync(new RoutineItem
                {
                    ExerciseName = "Deadlift",
                    DayOfWeek = DayOfWeek.Monday,
                    Order = 3
                });

                await _connection.InsertAsync(new RoutineItem
                {
                    ExerciseName = "Deadlift",
                    DayOfWeek = DayOfWeek.Tuesday,
                    Order = 1
                });

                // Seed WorkoutLog
                await _connection.InsertAsync(new WorkoutLog
                {
                    Date = DateTime.Now,
                    ExerciseName = "Bench Press",
                    Sets = 3,
                    Reps = 10,
                    Weight = 60.5
                });

                Debug.WriteLine("[DB] Dummy data seeded successfully.");
            }
        }
    }
}