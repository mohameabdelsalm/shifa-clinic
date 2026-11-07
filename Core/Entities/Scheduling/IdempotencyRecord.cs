using Core.Entities.Common;

namespace Core.Entities.Scheduling;

/// <summary>
/// Remembers the result of a request sent with an Idempotency-Key, so a retried request returns the first result
/// instead of booking twice. Written in the same transaction as the booking.
/// </summary>
public class IdempotencyRecord : TenantOwnedEntity
{
    public string Operation { get; set; } = "";
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public long ResultId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
