using Core.Entities.Common;

namespace Core.Entities.Tenancy;

/// <summary>A payment from a tenant, recorded manually by the Platform Admin (no payment gateway chosen yet, assumption A5).</summary>
[PlatformOwned]
public class SubscriptionPayment : BaseModel
{
    public long TenantSubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EGP";
    public DateTimeOffset PaidAt { get; set; }
    public string? Reference { get; set; }
}
