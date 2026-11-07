using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Infrastructure.Data;
using Shifa.Tenancy.Data;

namespace Shifa.Tenancy;

/// <summary>Entry point of the Tenancy module. Shifa.API calls <see cref="AddTenancyModule"/> once.</summary>
public static class TenancyModule
{
    public static IServiceCollection AddTenancyModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(TenancyModule).Assembly);
        services.AddModuleDbContext<TenancyDbContext>(TenancyDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(TenancyModule).Assembly, includeInternalTypes: true);

        // Repos are registered here as the module grows.
        return services;
    }
}
