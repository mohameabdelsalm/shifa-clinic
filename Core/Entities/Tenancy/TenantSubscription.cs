using Core.Entities.Common;
using Core.Enums.Tenancy;

namespace Core.Entities.Tenancy;

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
