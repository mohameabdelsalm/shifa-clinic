namespace Core.Interfaces.Common;

/// <summary>
/// The tenant the current request or background job acts for (ADR-002).
/// Scoped: one instance per request or job scope. It can be set once; setting a different
/// tenant afterwards throws, so code can never switch tenants in the middle of a request.
/// </summary>
public interface ITenantContext
{
    /// <summary>Null when there is no tenant (anonymous request, platform operation).</summary>
    long? TenantId { get; }

    bool HasTenant { get; }

    /// <summary>
    /// Sets the tenant. Only TenantResolutionHandler (from a validated token) and background
    /// job / event consumer runners (from the job's TenantId) may call this.
    /// </summary>
    void SetTenant(long tenantId);
}
