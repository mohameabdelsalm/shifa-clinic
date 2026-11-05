using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shifa.Clinic;

/// <summary>
/// Entry point of the Clinic module. Shifa.API calls <see cref="AddClinicModule"/> once;
/// everything the module needs (repos, ClinicDbContext, validators, controllers) is registered here,
/// so Program.cs never lists the module's classes.
/// </summary>
public static class ClinicModule
{
    public static IServiceCollection AddClinicModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Controllers live in this project's Controllers/ folder; tell MVC to look here.
        services.AddControllers().AddApplicationPart(typeof(ClinicModule).Assembly);

        // ClinicDbContext, repos and validators are added here as the module grows.
        return services;
    }
}
