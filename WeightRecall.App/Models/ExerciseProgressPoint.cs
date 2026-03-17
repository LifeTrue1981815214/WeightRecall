namespace WeightRecall.Models;

/// <summary>
/// Represents a data point for tracking exercise progress over time.
/// </summary>
/// <param name="Date">The date the progress was recorded.</param>
/// <param name="MaxWeight">The maximum weight lifted on this date.</param>
public record ExerciseProgressPoint(DateTime Date, double MaxWeight);
