using Core.Interfaces.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Common.TenantManager;

public sealed class TenantScopeFactory : ITenantScopeFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TenantScopeFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<T> RunAsync<T>(long tenantId, Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetTenant(tenantId);
        return await action(scope.ServiceProvider);
    }
}
