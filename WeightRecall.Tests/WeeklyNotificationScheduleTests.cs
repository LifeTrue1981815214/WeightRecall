using WeightRecall.Domain;
using WeightRecall.Services;

namespace WeightRecall.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// This is the schedule that ships. It is excluded from Debug builds by an
// #if DEBUG in NotificationService, which fires reminders 30 seconds out instead
// so they are easy to see while developing. That means nothing here is exercised
// by running the app locally -- these tests are the only coverage it gets.
// ─────────────────────────────────────────────────────────────────────────────
public class WeeklyNotificationScheduleTests
{
    // Reference points: 2026-09-16 is a Wednesday, 2026-09-15 a Tuesday.
    private static DateTime At(string dateTime) => DateTime.Parse(dateTime);

    [Theory]
    // Target later this week -> this week's occurrence.
    [InlineData("2026-09-16 08:00", DayOfWeek.Friday, "2026-09-18 10:00")]
    [InlineData("2026-09-16 23:59", DayOfWeek.Thursday, "2026-09-17 10:00")]
    // Target earlier in the week -> wraps to next week.
    [InlineData("2026-09-16 08:00", DayOfWeek.Tuesday, "2026-09-22 10:00")]
    [InlineData("2026-09-16 08:00", DayOfWeek.Monday, "2026-09-21 10:00")]
    public void ReturnsTheNextOccurrenceOfTheTargetWeekday(
        string now,
        DayOfWeek day,
        string expected
    )
    {
        Assert.Equal(
            At(expected),
            WeeklyNotificationSchedule.GetNextOccurrence(day, 10, 0, At(now))
        );
    }

    [Fact]
    public void OnTheTargetDayBeforeTheSlot_FiresToday()
    {
        // Wednesday 09:59, reminder is Wednesday 10:00 -- one minute away.
        Assert.Equal(
            At("2026-09-16 10:00"),
            WeeklyNotificationSchedule.GetNextOccurrence(
                DayOfWeek.Wednesday,
                10,
                0,
                At("2026-09-16 09:59")
            )
        );
    }

    [Fact]
    public void OnTheTargetDayAfterTheSlot_WaitsAFullWeek()
    {
        // The reminder must not fire immediately just because the day matches.
        Assert.Equal(
            At("2026-09-23 10:00"),
            WeeklyNotificationSchedule.GetNextOccurrence(
                DayOfWeek.Wednesday,
                10,
                0,
                At("2026-09-16 10:01")
            )
        );
    }

    [Fact]
    public void ExactlyOnTheSlot_WaitsAFullWeek()
    {
        // Boundary: at 10:00:00 the slot counts as gone, so it does not fire twice.
        Assert.Equal(
            At("2026-09-23 10:00"),
            WeeklyNotificationSchedule.GetNextOccurrence(
                DayOfWeek.Wednesday,
                10,
                0,
                At("2026-09-16 10:00")
            )
        );
    }

    [Fact]
    public void TheResultIsAlwaysInTheFuture()
    {
        // Every weekday, every hour of a reference day -- the scheduled time must never
        // be in the past, or the reminder fires the moment it is scheduled.
        DateTime baseDay = At("2026-09-16 00:00");

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            for (int hour = 0; hour < 24; hour++)
            {
                DateTime now = baseDay.AddHours(hour).AddMinutes(37);
                DateTime next = WeeklyNotificationSchedule.GetNextOccurrence(day, 10, 0, now);

                Assert.True(next > now, $"{day} scheduled at {next:O} which is not after {now:O}");
                Assert.Equal(day, next.DayOfWeek);
                Assert.Equal(new TimeSpan(10, 0, 0), next.TimeOfDay);
            }
        }
    }
}
