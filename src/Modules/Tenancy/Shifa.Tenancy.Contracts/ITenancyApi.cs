namespace Shifa.Tenancy.Contracts;

/// <summary>Plan entitlement codes. Limits are counts; features are on/off.</summary>
public static class EntitlementCodes
{
    public const string MaxBranches = "MaxBranches";
    public const string MaxDoctors = "MaxDoctors";
    public const string MaxEmployees = "MaxEmployees";
    public const string ModulePrefix = "Module.";
}

public sealed record TenantInfo(long Id, string Slug, string Name, string Status, bool IsActive);

/// <summary>A plan entitlement as seen by a tenant. Limit null with Enabled = true means unlimited.</summary>
public sealed record EntitlementValue(bool Enabled, int? Limit);

public interface ITenancyApi
{
    /// <summary>Null when the tenant doesn't exist. Cached briefly. IsActive is true for Trial and Active tenants.</summary>
    Task<TenantInfo?> GetTenantAsync(long tenantId, CancellationToken cancellationToken = default);

    Task<TenantInfo?> GetTenantBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Entitlement from the tenant's current subscription plan. A code missing from the plan returns
    /// Enabled = false for features, and no limit for limits (null) is treated as "not configured, unlimited".
    /// </summary>
    Task<EntitlementValue> GetEntitlementAsync(long tenantId, string code, CancellationToken cancellationToken = default);
}
