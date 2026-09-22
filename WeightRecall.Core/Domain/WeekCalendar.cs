namespace WeightRecall.Domain;

/// <summary>
/// Locates the week a date belongs to.
/// </summary>
/// <remarks>
/// Weeks here start on Monday, which is what the main page's day strip shows.
/// </remarks>
public static class WeekCalendar
{
    /// <summary>
    /// Calculates the date of the Monday for the week containing the specified date.
    /// </summary>
    /// <param name="date">The reference date.</param>
    /// <returns>The <see cref="DateTime"/> representing the Monday of that week.</returns>
    public static DateTime GetMonday(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Generates a list of all dates in the week starting from the given Monday.
    /// </summary>
    /// <param name="monday">The start of the week.</param>
    /// <returns>A list containing 7 <see cref="DateTime"/> objects for the week.</returns>
    public static List<DateTime> GetDaysOfWeek(DateTime monday)
    {
        List<DateTime> days = [];
        for (int i = 0; i < 7; i++)
        {
            days.Add(monday.AddDays(i));
        }
        return days;
    }
}
