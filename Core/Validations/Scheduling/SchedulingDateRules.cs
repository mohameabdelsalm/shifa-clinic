namespace Core.Validations.Scheduling;

/// <summary>
/// Outer bounds for dates and times sent by clients. Real bookings are limited much more tightly (no past starts,
/// 90-day horizon) by the availability check; these bounds only keep extreme values (e.g. 9999-12-31) from
/// overflowing time-zone math and turning into 500s.
/// </summary>
public static class SchedulingDateRules
{
    private static readonly DateOnly MinDate = new(2000, 1, 1);
    private static readonly DateOnly MaxDate = new(2100, 12, 31);

    public static bool IsSaneDate(DateOnly date) => date >= MinDate && date <= MaxDate;

    public static bool IsSaneInstant(DateTimeOffset instant) => IsSaneDate(DateOnly.FromDateTime(instant.UtcDateTime));
}
