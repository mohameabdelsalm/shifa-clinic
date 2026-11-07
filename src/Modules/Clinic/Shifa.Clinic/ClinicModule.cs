using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Clinic.Data;
using Shifa.Infrastructure.Data;

namespace Shifa.Clinic;

/// <summary>
/// Entry point of the Clinic module. Shifa.API calls <see cref="AddClinicModule"/> once;
/// everything the module needs (repos, ClinicDbContext, validators, controllers) is registered here,
/// so Program.cs never lists the module's classes.
/// </summary>
public static class ClinicModule
{
    public static IServiceCollection AddClinicModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(ClinicModule).Assembly);
        services.AddModuleDbContext<ClinicDbContext>(ClinicDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(ClinicModule).Assembly, includeInternalTypes: true);

        // Repos are registered here as the module grows.
        return services;
    }
}
