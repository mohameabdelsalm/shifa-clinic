using Core.Interfaces.Common;
using Infrastructure.Common.CurrentUserManager;
using Infrastructure.Common.TenantManager;
using Infrastructure.Data.Interceptors;
using Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure;

public static class InfrastructureModule
{
    /// <summary>Shared services every module depends on.</summary>
    public static IServiceCollection AddShifaInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAccessContext, AccessContext>();
        services.AddSingleton<ITenantScopeFactory, TenantScopeFactory>();
        services.AddScoped<AuditTenantInterceptor>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Local fake (ADR-006); a real provider replaces ISmsSender in production.
        services.AddSingleton<InMemorySmsOutbox>();
        services.TryAddSingleton<ISmsSender, FakeSmsSender>();

        return services;
    }
}
