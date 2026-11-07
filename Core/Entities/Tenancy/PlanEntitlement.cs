using Core.Entities.Common;

namespace Core.Entities.Tenancy;

/// <summary>One limit (MaxBranches...) or feature (Module.Billing...) of a plan. LimitValue null with Enabled = unlimited.</summary>
[PlatformOwned]
public class PlanEntitlement : BaseModel
{
    public long PlanId { get; set; }
    public string Code { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int? LimitValue { get; set; }
}
