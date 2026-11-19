namespace Core.DTO.Tenancy;

public class PlanDto
{
    public long Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string BillingPeriod { get; set; } = "";
    public int TrialDays { get; set; }
    public bool IsActive { get; set; }
    public List<PlanEntitlementDto> Entitlements { get; set; } = [];
}
