using Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;

public static class ModuleDbContextExtensions
{
    public const string ConnectionStringName = "Shifa";

    /// <summary>
    /// Registers a module DbContext on the shared database: its own migrations history table in
    /// its schema, the tenant/audit interceptor, and a readiness health check.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : ModuleDbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            // Read at resolution time so test hosts can override the connection string.
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

            options
                .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", schema))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditTenantInterceptor>());
        });

        services.AddHealthChecks().AddDbContextCheck<TContext>(name: typeof(TContext).Name, tags: ["ready"]);

        return services;
    }
}
