using Core.DTO.Scheduling;
using Core.DTO.Clinic;
using Core.Interfaces.Clinic;
using Infrastructure.Services.Scheduling;

namespace Shifa.Tests.Unit.Scheduling;

public class AvailabilityCalculatorTests
{
    // A fixed +02:00 zone and a +02:00 zone with daylight time (+03:00) from 10 April 02:00 to 20 October 02:00.
    // Custom zones keep the tests independent of the machine's time zone database.
    private static readonly TimeZoneInfo Fixed = TimeZoneInfo.CreateCustomTimeZone("Test+2", TimeSpan.FromHours(2), "Test+2", "Test+2");

    private static readonly TimeZoneInfo WithDst = TimeZoneInfo.CreateCustomTimeZone(
        "TestDst", TimeSpan.FromHours(2), "TestDst", "TestStd", "TestDst",
        [
            TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
                DateTime.MinValue.Date, DateTime.MaxValue.Date, TimeSpan.FromHours(1),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 4, 10),
                TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 20)),
        ]);

    private static readonly DateOnly Day = new(2030, 1, 7); // a Monday, no daylight time
    private static readonly DateTimeOffset Always = DateTimeOffset.MinValue;
    private static readonly DateTimeOffset Never = DateTimeOffset.MaxValue;

    private static ScheduleWindow Window(DayOfWeek day, int fromHour, int fromMinute, int toHour, int toMinute, int slot) =>
        new(day, new TimeOnly(fromHour, fromMinute), new TimeOnly(toHour, toMinute), slot);

    private static DateTimeOffset Utc(DateOnly date, int hour, int minute) =>
        new(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero);

    private static List<string> LocalStarts(IEnumerable<AvailableSlot> slots) => slots.Select(s => s.LocalStart.ToString("HH:mm")).ToList();

    [Fact]
    public void Cuts_window_into_slots_and_converts_to_utc()
    {
        var slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Monday, 9, 0, 10, 0, 15)], 15, [], Always, Never);

        Assert.Equal(["09:00", "09:15", "09:30", "09:45"], LocalStarts(slots));
        Assert.Equal(Utc(Day, 7, 0), slots[0].StartUtc);
        Assert.Equal(Utc(Day, 7, 15), slots[0].EndUtc);
        Assert.All(slots, s => Assert.Equal(TimeSpan.Zero, s.StartUtc.Offset));
    }

    [Fact]
    public void Service_longer_than_a_slot_takes_whole_consecutive_slots()
    {
        // 20 minutes on a 15-minute grid needs two slots (30 minutes); the last start must still end inside the window.
        var slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Monday, 9, 0, 10, 0, 15)], 20, [], Always, Never);

        Assert.Equal(["09:00", "09:15", "09:30"], LocalStarts(slots));
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromMinutes(30), s.EndUtc - s.StartUtc));
    }

    [Fact]
    public void A_visit_never_spans_two_windows()
    {
        var schedule = new[] { Window(DayOfWeek.Monday, 9, 0, 9, 30, 15), Window(DayOfWeek.Monday, 9, 30, 10, 0, 15) };

        var slots = AvailabilityCalculator.Calculate(Day, Fixed, schedule, 30, [], Always, Never);

        Assert.Equal(["09:00", "09:30"], LocalStarts(slots));
    }

    [Fact]
    public void Busy_ranges_remove_overlapping_starts_but_not_touching_ones()
    {
        // Busy 09:20–09:40 local = 07:20–07:40 UTC.
        var busy = new[] { new TimeRange(Utc(Day, 7, 20), Utc(Day, 7, 40)) };

        var slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Monday, 9, 0, 10, 0, 15)], 15, busy, Always, Never);

        Assert.Equal(["09:00", "09:45"], LocalStarts(slots));

        var touching = new[] { new TimeRange(Utc(Day, 7, 15), Utc(Day, 7, 30)) };
        slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Monday, 9, 0, 10, 0, 15)], 15, touching, Always, Never);

        Assert.Equal(["09:00", "09:30", "09:45"], LocalStarts(slots));
    }

    [Fact]
    public void Starts_outside_the_booking_range_are_dropped()
    {
        var slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Monday, 9, 0, 10, 0, 15)], 15, [],
            notBefore: Utc(Day, 7, 10), notAfter: Utc(Day, 7, 30));

        Assert.Equal(["09:15", "09:30"], LocalStarts(slots));
    }

    [Fact]
    public void Other_days_of_the_week_have_no_slots()
    {
        var slots = AvailabilityCalculator.Calculate(Day, Fixed, [Window(DayOfWeek.Tuesday, 9, 0, 10, 0, 15)], 15, [], Always, Never);

        Assert.Empty(slots);
    }

    [Fact]
    public void Spring_forward_skips_local_times_that_do_not_exist()
    {
        var date = new DateOnly(2030, 4, 10);

        var slots = AvailabilityCalculator.Calculate(date, WithDst, [Window(date.DayOfWeek, 1, 0, 4, 0, 30)], 30, [], Always, Never);

        // 02:00 and 02:30 never happen on this day.
        Assert.Equal(["01:00", "01:30", "03:00", "03:30"], LocalStarts(slots));
        Assert.Equal(Utc(date.AddDays(-1), 23, 0), slots[0].StartUtc); // +02:00
        Assert.Equal(Utc(date, 0, 0), slots[2].StartUtc);              // +03:00
    }

    [Fact]
    public void Fall_back_uses_the_first_occurrence_of_a_repeated_local_time()
    {
        var date = new DateOnly(2030, 10, 20);

        var slots = AvailabilityCalculator.Calculate(date, WithDst, [Window(date.DayOfWeek, 0, 0, 3, 0, 30)], 30, [], Always, Never);

        Assert.Equal(["00:00", "00:30", "01:00", "01:30", "02:00", "02:30"], LocalStarts(slots));
        Assert.Equal(Utc(date.AddDays(-1), 22, 0), slots[2].StartUtc); // 01:00 first occurrence, +03:00
        Assert.Equal(Utc(date, 0, 0), slots[4].StartUtc);              // 02:00 standard time, +02:00
        Assert.Equal(slots.Count, slots.Select(s => s.StartUtc).Distinct().Count());
    }

    [Fact]
    public void Ticks_cover_the_range_in_five_minute_steps()
    {
        var start = Utc(Day, 7, 0);

        var ticks = AvailabilityCalculator.Ticks(start, start.AddMinutes(30));

        Assert.Equal(6, ticks.Count);
        Assert.Equal(start, ticks[0]);
        Assert.Equal(start.AddMinutes(25), ticks[^1]);
        Assert.Throws<ArgumentException>(() => AvailabilityCalculator.Ticks(start.AddMinutes(3), start.AddMinutes(30)));
    }

    [Fact]
    public void Day_range_follows_daylight_time_changes()
    {
        var (springFrom, springTo) = AvailabilityCalculator.DayRangeUtc(new DateOnly(2030, 4, 10), WithDst);
        var (fallFrom, fallTo) = AvailabilityCalculator.DayRangeUtc(new DateOnly(2030, 10, 20), WithDst);
        var (plainFrom, plainTo) = AvailabilityCalculator.DayRangeUtc(Day, Fixed);

        Assert.Equal(TimeSpan.FromHours(23), springTo - springFrom);
        Assert.Equal(TimeSpan.FromHours(25), fallTo - fallFrom);
        Assert.Equal(TimeSpan.FromHours(24), plainTo - plainFrom);
        Assert.Equal(Utc(Day.AddDays(-1), 22, 0), plainFrom);
    }

    [Fact]
    public void Local_date_of_an_instant_uses_the_branch_zone()
    {
        Assert.Equal(Day, AvailabilityCalculator.LocalDateOf(Utc(Day.AddDays(-1), 22, 30), Fixed));
        Assert.Equal(Day.AddDays(-1), AvailabilityCalculator.LocalDateOf(Utc(Day.AddDays(-1), 21, 59), Fixed));
    }
}
