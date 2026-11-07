using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Infrastructure.Data;
using Shifa.Tenancy.Contracts;
using Shifa.Tenancy.Data;
using Shifa.Tenancy.Interfaces;
using Shifa.Tenancy.Repositories;

namespace Shifa.Tenancy;

/// <summary>Entry point of the Tenancy module. Shifa.API calls <see cref="AddTenancyModule"/> once.</summary>
public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(TenancyModule).Assembly);
        services.AddModuleDbContext<TenancyDbContext>(TenancyDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(TenancyModule).Assembly, includeInternalTypes: true);

        services.AddScoped<ITenancyApi, TenancyApi>();
        services.AddScoped<IPlanRepo, PlanRepo>();
        services.AddScoped<ITenantRepo, TenantRepo>();

        return services;
    }
}
