using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace WeightRecall.Models
{
    public class WorkoutLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ExerciseId { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public double Weight { get; set; }
    }
}
