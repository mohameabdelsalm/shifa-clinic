using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Shifa.Core.Interfaces.Common;
using Shifa.Identity.Contracts;
using Shifa.Identity.Data;
using Shifa.Identity.Entities;
using Shifa.Identity.Interfaces;
using Shifa.Identity.Repositories;
using Shifa.Identity.Services;
using Shifa.Infrastructure.Data;

namespace Shifa.Identity;

/// <summary>Entry point of the Identity module. Shifa.API calls <see cref="AddIdentityModule"/> once.</summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddControllers().AddApplicationPart(typeof(IdentityModule).Assembly);
        services.AddModuleDbContext<IdentityDbContext>(IdentityDbContext.SchemaName);
        services.AddValidatorsFromAssembly(typeof(IdentityModule).Assembly, includeInternalTypes: true);

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<MembershipAccessCache>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IIdentityApi, IdentityApi>();
        services.AddScoped<IAuthRepo, AuthRepo>();
        services.AddScoped<IRoleRepo, RoleRepo>();
        services.AddScoped<IStaffRepo, StaffRepo>();

        return services;
    }
}
