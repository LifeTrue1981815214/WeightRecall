namespace WeightRecall.Services;

/// <summary>
/// Works out when a weekly reminder should next fire.
/// </summary>
/// <remarks>
/// Lives here, takes the current time as a parameter, and is public on purpose. It used to be a
/// private helper inside the Android notification service, compiled out of Debug builds entirely
/// -- so the scheduling that actually ships never ran during development and could not be tested.
/// </remarks>
public static class WeeklyNotificationSchedule
{
    /// <summary>
    /// Returns the next moment the given weekday occurs at the given time of day.
    /// </summary>
    /// <remarks>
    /// When today is the target day, the slot counts only if it has not already passed; at or
    /// after it, the reminder moves to next week rather than firing immediately.
    /// </remarks>
    /// <param name="day">The weekday the reminder belongs to.</param>
    /// <param name="hour">Hour of day, 0-23.</param>
    /// <param name="minute">Minute of the hour, 0-59.</param>
    /// <param name="now">The current local time.</param>
    /// <returns>The next occurrence, always strictly in the future relative to <paramref name="now"/>.</returns>
    public static DateTime GetNextOccurrence(DayOfWeek day, int hour, int minute, DateTime now)
    {
        DateTime slotToday = new(now.Year, now.Month, now.Day, hour, minute, 0);

        int daysUntil = (((int)day - (int)now.DayOfWeek) + 7) % 7;

        // Today is the target day but the slot has been and gone -- wait a full week.
        if (daysUntil == 0 && now.TimeOfDay >= new TimeSpan(hour, minute, 0))
        {
            daysUntil = 7;
        }

        return slotToday.AddDays(daysUntil);
    }
}
