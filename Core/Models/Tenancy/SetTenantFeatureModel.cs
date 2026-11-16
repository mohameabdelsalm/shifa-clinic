namespace Core.Models.Tenancy;

public class SetTenantFeatureModel
{
    public long TenantId { get; set; }

    /// <summary>One of FeatureCodes, e.g. "Feature.OnlinePayments".</summary>
    public string Code { get; set; } = "";

    /// <summary>true/false sets an override for this tenant; null removes it (back to the plan).</summary>
    public bool? Enabled { get; set; }
}
