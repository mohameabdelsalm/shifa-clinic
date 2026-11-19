using Core.Entities.Common;

namespace Core.Entities.Billing;

/// <summary>Remembers a payment/refund request by its Idempotency-Key, so a retry returns the same result (24 h).</summary>
public class BillingIdempotencyRecord : TenantOwnedEntity
{
    public string Operation { get; set; } = "";
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public long ResultId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
