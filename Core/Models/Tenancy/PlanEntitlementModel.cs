namespace Core.Models.Tenancy;

public class PlanEntitlementModel
{
    /// <summary>MaxBranches, MaxDoctors, MaxEmployees, or Module.&lt;Name&gt;.</summary>
    public string Code { get; set; } = "";
    public bool Enabled { get; set; } = true;

    /// <summary>Null = unlimited (for limits) .</summary>
    public int? LimitValue { get; set; }
}
