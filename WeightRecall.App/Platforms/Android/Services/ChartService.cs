using Microcharts;
using SkiaSharp;
using WeightRecall.Models;
using WeightRecall.Services;

namespace WeightRecall.Platforms.Android.Services;

public class ChartService : IChartService
{
    public Chart GenerateLineChart(IEnumerable<ExerciseProgressPoint> points, AppTheme theme)
    {
        bool isDark = theme == AppTheme.Dark;

        SKColor mainTextColor = isDark ? SKColors.White : SKColors.Black;
        SKColor backgroundColor = isDark ? SKColors.Black : SKColors.White;

        SKColor accentColor = isDark ? SKColors.White : SKColors.Black;

        List<ChartEntry> entries = points
            .Select(p => new ChartEntry((float)p.MaxWeight)
            {
                Label = p.Date.ToString("dd/MM"),
                ValueLabel = p.MaxWeight.ToString("0.##"),
                Color = accentColor,
                ValueLabelColor = mainTextColor,
            })
            .ToList();

        return new LineChart
        {
            Entries = entries,
            LineMode = LineMode.Straight,
            LineSize = 8,
            PointMode = PointMode.Circle,
            PointSize = 19,
            LabelTextSize = 40,
            LabelOrientation = Orientation.Horizontal,
            ValueLabelOrientation = Orientation.Horizontal,
            LabelColor = mainTextColor,
            BackgroundColor = backgroundColor,
        };
    }
}
