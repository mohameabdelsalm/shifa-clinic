namespace Core.DTO.Tenancy;

/// <summary>A feature as it applies to a tenant, and where the value comes from.</summary>
public class TenantFeatureDto
{
    public string Code { get; set; } = "";
    public bool Enabled { get; set; }

    /// <summary>Override (set for this tenant), Plan (the plan's entitlement) or Default.</summary>
    public string Source { get; set; } = "";
}
