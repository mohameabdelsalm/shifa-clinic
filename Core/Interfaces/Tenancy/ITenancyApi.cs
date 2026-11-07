using Core.DTO.Tenancy;

namespace Core.Interfaces.Tenancy;

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
