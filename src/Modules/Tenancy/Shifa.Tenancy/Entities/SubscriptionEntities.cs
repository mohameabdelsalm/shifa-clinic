using Shifa.Core.Entities.Common;
using Shifa.Tenancy.Enums;

namespace Shifa.Tenancy.Entities;

/// <summary>A commercial plan. Prices and limits are data set by the Platform Admin, never hardcoded.</summary>
[PlatformOwned]
public class Plan : BaseModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public BillingPeriodEnum BillingPeriod { get; set; } = BillingPeriodEnum.Monthly;
    public int TrialDays { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>One limit (MaxBranches...) or feature (Module.Billing...) of a plan. LimitValue null with Enabled = unlimited.</summary>
[PlatformOwned]
public class PlanEntitlement : BaseModel
{
    public long PlanId { get; set; }
    public string Code { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int? LimitValue { get; set; }
}

/// <summary>Which plan a tenant is on and for how long. Managed by the Platform Admin, so platform-owned.</summary>
[PlatformOwned]
public class TenantSubscription : BaseModel
{
    public long TenantId { get; set; }
    public long PlanId { get; set; }
    public SubscriptionStatusEnum Status { get; set; } = SubscriptionStatusEnum.Trial;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
}

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
