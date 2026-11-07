using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Infrastructure.Data;
using Shifa.Scheduling.Data;
using Shifa.Scheduling.Interfaces;
using Shifa.Scheduling.Repositories;
using Shifa.Scheduling.Services;

namespace Shifa.Scheduling;

/// <summary>Entry point of the Scheduling module. Shifa.API calls <see cref="AddSchedulingModule"/> once.</summary>
public static class SchedulingModule
{
    public static IServiceCollection AddSchedulingModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(SchedulingModule).Assembly);
        services.AddModuleDbContext<SchedulingDbContext>(SchedulingDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(SchedulingModule).Assembly, includeInternalTypes: true);

        services.AddScoped<BookingService>();
        services.AddScoped<AppointmentRepo>();
        services.AddScoped<IAppointmentRepo>(sp => sp.GetRequiredService<AppointmentRepo>());
        services.AddScoped<IPortalAppointmentRepo, PortalAppointmentRepo>();

        return services;
    }
}
