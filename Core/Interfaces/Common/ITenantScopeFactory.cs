namespace Core.Interfaces.Common;

/// <summary>
/// Runs code in a new DI scope whose ITenantContext is set to the given tenant.
/// Used where the tenant is known from a validated source but the current request has no (or another) tenant context:
/// login, onboarding, membership lookups, and later background jobs and event consumers.
/// </summary>
public interface ITenantScopeFactory
{
    Task<T> RunAsync<T>(long tenantId, Func<IServiceProvider, Task<T>> action);
}
