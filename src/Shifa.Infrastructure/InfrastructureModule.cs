using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shifa.Core.Interfaces.Common;
using Shifa.Infrastructure.Common.CurrentUserManager;
using Shifa.Infrastructure.Common.TenantManager;
using Shifa.Infrastructure.Data.Interceptors;

namespace Shifa.Infrastructure;

public static class InfrastructureModule
{
    /// <summary>Shared services every module depends on.</summary>
    public static IServiceCollection AddShifaInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<AuditTenantInterceptor>();

        return services;
    }
}
