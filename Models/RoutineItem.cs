using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace WeightRecall.Models
{
    public class RoutineItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public int ExerciseId { get; set; }
        public int Order { get; set; }
    }
}
