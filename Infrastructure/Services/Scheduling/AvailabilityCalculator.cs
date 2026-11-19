using Core.DTO.Scheduling;
using Core.DTO.Clinic;

namespace Infrastructure.Services.Scheduling;

/// <summary>
/// Computes the free start times of one doctor, at one branch, for one service, on one local date (ADR-008).
/// Pure: no database, no clock — everything comes in as arguments, so it is unit-tested directly.
/// <list type="number">
/// <item>Take the schedule windows for the date's day of week (branch-local times).</item>
/// <item>Cut each window into slots; a start is offered only if ceil(duration / slot) consecutive slots fit inside the same window.</item>
/// <item>Convert to UTC with the branch's time zone. Local times that don't exist (DST gap) are skipped;
/// ambiguous ones (DST fall-back) use the first occurrence.</item>
/// <item>Drop starts outside [notBefore, notAfter] and starts overlapping a busy range (time off, other appointments at any branch).</item>
/// </list>
/// </summary>
public static class AvailabilityCalculator
{
    public static IReadOnlyList<AvailableSlot> Calculate(
        DateOnly localDate,
        TimeZoneInfo timeZone,
        IReadOnlyList<ScheduleWindow> schedule,
        int serviceDurationMinutes,
        IReadOnlyList<TimeRange> busy,
        DateTimeOffset notBefore,
        DateTimeOffset notAfter)
    {
        if (serviceDurationMinutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(serviceDurationMinutes));

        var result = new List<AvailableSlot>();

        foreach (var window in schedule.Where(w => w.DayOfWeek == localDate.DayOfWeek && w.SlotMinutes > 0))
        {
            var slotsNeeded = (serviceDurationMinutes + window.SlotMinutes - 1) / window.SlotMinutes;
            var length = TimeSpan.FromMinutes(slotsNeeded * window.SlotMinutes);
            var windowStart = localDate.ToDateTime(window.StartTime);
            var windowEnd = localDate.ToDateTime(window.EndTime);
            // The visit must also end inside the window in real time: across a DST gap, wall-clock and elapsed time differ.
            var windowEndUtc = ToUtc(SkipGap(windowEnd, timeZone), timeZone);

            for (var localStart = windowStart; localStart + length <= windowEnd; localStart = localStart.AddMinutes(window.SlotMinutes))
            {
                if (timeZone.IsInvalidTime(localStart))
                    continue;

                var startUtc = ToUtc(localStart, timeZone);
                var endUtc = startUtc + length;

                if (endUtc > windowEndUtc)
                    continue;
                if (startUtc < notBefore || startUtc > notAfter)
                    continue;
                if (busy.Any(b => b.From < endUtc && startUtc < b.To))
                    continue;

                result.Add(new AvailableSlot(startUtc, endUtc, localStart));
            }
        }

        return result.OrderBy(s => s.StartUtc).DistinctBy(s => s.StartUtc).ToList();
    }

    /// <summary>The 5-minute UTC ticks covered by [startUtc, endUtc); each becomes an AppointmentSlot row.</summary>
    public static IReadOnlyList<DateTimeOffset> Ticks(DateTimeOffset startUtc, DateTimeOffset endUtc)
    {
        if (startUtc.UtcTicks % TimeSpan.FromMinutes(SchedulingRules.TickMinutes).Ticks != 0)
            throw new ArgumentException("Start must be on the 5-minute grid.", nameof(startUtc));

        var ticks = new List<DateTimeOffset>();
        for (var t = startUtc.ToUniversalTime(); t < endUtc; t = t.AddMinutes(SchedulingRules.TickMinutes))
            ticks.Add(t);
        return ticks;
    }

    /// <summary>The branch-local calendar date of a UTC instant.</summary>
    public static DateOnly LocalDateOf(DateTimeOffset utc, TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, timeZone).DateTime);

    /// <summary>The UTC range covering a whole local date (handles 23- and 25-hour days).</summary>
    public static (DateTimeOffset From, DateTimeOffset To) DayRangeUtc(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var from = ToUtc(SkipGap(localDate.ToDateTime(TimeOnly.MinValue), timeZone), timeZone);
        var to = ToUtc(SkipGap(localDate.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone), timeZone);
        return (from, to);
    }

    private static DateTime SkipGap(DateTime local, TimeZoneInfo timeZone)
    {
        while (timeZone.IsInvalidTime(local))
            local = local.AddMinutes(SchedulingRules.TickMinutes);
        return local;
    }

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        // Fall-back: the first occurrence of an ambiguous local time is the one with the larger (daylight) offset.
        var offset = timeZone.IsAmbiguousTime(local) ? timeZone.GetAmbiguousTimeOffsets(local).Max() : timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
