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

        public DatabaseContext()
        {
            //var databasePath = Path.Combine(FileSystem.AppDataDirectory, "WeightRecall.db3");
            // Use this ONLY for debugging to make the file visible in File Pickers
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var databasePath = Path.Combine(Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath, "WeightRecall.db3");
            Debug.WriteLine($"[DB] Database Path: {databasePath}");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            _connection = new SQLiteAsyncConnection(databasePath);

            // Initialize all tables
            _connection.CreateTableAsync<RoutineItem>().Wait();
            _connection.CreateTableAsync<WorkoutLog>().Wait();
            _connection.CreateTableAsync<ExerciseList>().Wait();
            Debug.WriteLine("[DB] Tables created successfully.");

            SeedData().Wait();
        }

        public SQLiteAsyncConnection Connection => _connection;

        private async Task SeedData()
        {
            // Only seed if ExerciseList is empty
            if (await _connection.Table<ExerciseList>().CountAsync() == 0)
            {
                Debug.WriteLine("[DB] Seeding dummy data...");
                var exercises = new List<ExerciseList>
                {
                    new ExerciseList { ExerciseName = "Bench Press" },
                    new ExerciseList { ExerciseName = "Squat" },
                    new ExerciseList { ExerciseName = "Deadlift" }
                };
                await _connection.InsertAllAsync(exercises);

                // Get the generated ID for Bench Press
                var benchPress = await _connection.Table<ExerciseList>().FirstOrDefaultAsync(e => e.ExerciseName == "Bench Press");

                if (benchPress != null)
                {
                    // Seed RoutineItem (Monday = 1)
                    await _connection.InsertAsync(new RoutineItem
                    {
                        DayOfWeek = 1,
                        ExerciseId = benchPress.Id,
                        Order = 1
                    });

                    // Seed WorkoutLog
                    await _connection.InsertAsync(new WorkoutLog
                    {
                        Date = DateTime.Now,
                        ExerciseId = benchPress.Id,
                        Sets = 3,
                        Reps = 10,
                        Weight = 60.5
                    });
                }
                Debug.WriteLine("[DB] Dummy data seeded successfully.");
            }
        }
    }
}