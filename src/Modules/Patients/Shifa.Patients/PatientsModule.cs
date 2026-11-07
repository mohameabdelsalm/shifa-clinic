using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Infrastructure.Data;
using Shifa.Patients.Contracts;
using Shifa.Patients.Data;
using Shifa.Patients.Interfaces;
using Shifa.Patients.Repositories;

namespace Shifa.Patients;

/// <summary>Entry point of the Patients module. Shifa.API calls <see cref="AddPatientsModule"/> once.</summary>
public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(PatientsModule).Assembly);
        services.AddModuleDbContext<PatientsDbContext>(PatientsDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(PatientsModule).Assembly, includeInternalTypes: true);

        services.AddScoped<IPatientsApi, PatientsApi>();
        services.AddScoped<IPatientRepo, PatientRepo>();
        services.AddScoped<IPortalPatientRepo, PortalPatientRepo>();

        return services;
    }
}
