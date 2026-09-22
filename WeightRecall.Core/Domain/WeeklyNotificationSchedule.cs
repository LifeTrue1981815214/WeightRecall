namespace WeightRecall.Domain;

/// <summary>
/// Works out when a weekly reminder should next fire.
/// </summary>
public static class WeeklyNotificationSchedule
{
    /// <summary>
    /// Returns the next moment the given weekday occurs at the given time of day.
    /// </summary>
    /// <remarks>
    /// On the target day the slot counts only if it has not already passed, so the result is
    /// always strictly after <paramref name="now"/>.
    /// </remarks>
    public static DateTime GetNextOccurrence(DayOfWeek day, int hour, int minute, DateTime now)
    {
        DateTime slotToday = new(now.Year, now.Month, now.Day, hour, minute, 0);

        int daysUntil = (((int)day - (int)now.DayOfWeek) + 7) % 7;

        // Target day, but the slot has been and gone -- wait a full week.
        if (daysUntil == 0 && now.TimeOfDay >= new TimeSpan(hour, minute, 0))
        {
            daysUntil = 7;
        }

        return slotToday.AddDays(daysUntil);
    }
}
