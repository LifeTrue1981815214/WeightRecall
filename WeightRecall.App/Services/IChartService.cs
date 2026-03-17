using Microcharts;
using WeightRecall.Models;

namespace WeightRecall.Services;

public interface IChartService
{
    Chart GenerateLineChart(IEnumerable<ExerciseProgressPoint> points, AppTheme theme);
}
