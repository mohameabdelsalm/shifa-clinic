using Core.Entities.Common;
using Core.Enums.Tenancy;

namespace Core.Entities.Tenancy;

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
