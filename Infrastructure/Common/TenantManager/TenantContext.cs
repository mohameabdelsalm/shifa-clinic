using Core.Interfaces.Common;

namespace Infrastructure.Common.TenantManager;

/// <summary>
/// Scoped holder of the current tenant. Registered as Scoped, never Singleton:
/// a singleton would leak one request's tenant into another request.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public long? TenantId { get; private set; }

    public bool HasTenant => TenantId is not null;

    public void SetTenant(long tenantId)
    {
        if (tenantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(tenantId), "Tenant id must be positive.");

        if (TenantId is not null && TenantId != tenantId)
            throw new InvalidOperationException("The tenant context is already set to a different tenant.");

        TenantId = tenantId;
    }
}
