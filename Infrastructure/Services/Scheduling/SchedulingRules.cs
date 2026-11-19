namespace Infrastructure.Services.Scheduling;

/// <summary>Business constants of booking (assumptions A9, A10, A19). They move to tenant settings later.</summary>
public static class SchedulingRules
{
    public const int TickMinutes = 5;
    public static readonly TimeSpan BookingHorizon = TimeSpan.FromDays(90);
    public static readonly TimeSpan CheckInOpensBefore = TimeSpan.FromHours(2);
    public static readonly TimeSpan PortalCancelUntilBefore = TimeSpan.FromHours(3);
    public static readonly TimeSpan NoShowAfter = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan IdempotencyLifetime = TimeSpan.FromHours(24);
    public const int NoShowsBeforePending = 3;
}
