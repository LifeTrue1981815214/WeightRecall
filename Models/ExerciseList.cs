using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace WeightRecall.Models
{
    public class ExerciseList
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string ExerciseName { get; set; }
    }
}
