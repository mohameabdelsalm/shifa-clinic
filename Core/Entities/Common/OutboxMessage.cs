namespace Core.Entities.Common;

/// <summary>
/// An integration event waiting to be dispatched (ADR-005). Lives in the publishing module's schema and is saved in the
/// same SaveChanges as the business change. Infrastructure, not a business entity: no tenant filter (the dispatcher
/// reads every tenant's messages and sets the tenant from <see cref="TenantId"/>).
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; }
    public long? TenantId { get; set; }

    /// <summary>The event class name, e.g. "AppointmentBooked".</summary>
    public string Type { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }

    /// <summary>Lease: a dispatcher instance owns the message until then.</summary>
    public DateTimeOffset? LockedUntil { get; set; }
    public string? LastError { get; set; }

    /// <summary>Set after the last failed attempt: the message is left for investigation and never retried automatically.</summary>
    public DateTimeOffset? PoisonedAt { get; set; }
}
