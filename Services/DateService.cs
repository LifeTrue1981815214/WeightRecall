namespace WeightRecall.Services;

public class DateService
{
    public DateTime GetMonday(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    public List<DateTime> GetDaysOfWeek(DateTime monday)
    {
        List<DateTime> days = [];
        for (int i = 0; i < 7; i++)
        {
            days.Add(monday.AddDays(i));
        }
        return days;
    }
}
