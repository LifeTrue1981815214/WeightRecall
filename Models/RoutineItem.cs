using SQLite;

namespace WeightRecall.Models;

[Table("RoutineItems")]
public class RoutineItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string ExerciseName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public int Order { get; set; }
}
