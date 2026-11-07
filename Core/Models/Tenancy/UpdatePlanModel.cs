namespace Core.Models.Tenancy;

public class UpdatePlanModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int TrialDays { get; set; }
    public bool IsActive { get; set; } = true;
    public List<PlanEntitlementModel> Entitlements { get; set; } = [];
}
