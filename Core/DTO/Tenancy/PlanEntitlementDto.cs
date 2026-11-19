namespace Core.DTO.Tenancy;

public class PlanEntitlementDto
{
    public string Code { get; set; } = "";
    public bool Enabled { get; set; }
    public int? LimitValue { get; set; }
}
