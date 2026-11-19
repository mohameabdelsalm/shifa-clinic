using Hangfire.SqlServer;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Shifa.Extensions;

/// <summary>
/// "dotnet Shifa.dll --migrate" (ADR-017): applies every module's pending migrations, in dependency order, then exits.
/// Run on purpose at deploy time; migrations never run automatically at startup. Migrations only add or change; they
/// never drop the database. Review the pending list (printed first) before running it against production.
/// </summary>
public static class MigrationCommand
{
    private static readonly Type[] Contexts =
    [
        typeof(TenancyDbContext), typeof(IdentityDbContext), typeof(ClinicDbContext), typeof(PatientsDbContext),
        typeof(SchedulingDbContext), typeof(ClinicalDbContext), typeof(BillingDbContext), typeof(NotificationsDbContext),
    ];

    public static async Task RunAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        foreach (var type in Contexts)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(type);
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
            Console.WriteLine($"{type.Name}: {(pending.Count == 0 ? "up to date" : string.Join(", ", pending))}");
            if (pending.Count > 0)
                await context.Database.MigrateAsync();
        }

        // Hangfire's own tables (schema "hangfire"), so the running API needs no DDL rights (ADR-017).
        var connectionString = scope.ServiceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ModuleDbContextExtensions.ConnectionStringName);
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync();
            SqlServerObjectsInstaller.Install(connection, JobServiceExtensions.SchemaName, enableHeavyMigrations: true);
        }
        Console.WriteLine("hangfire: schema installed/up to date");
        Console.WriteLine("Migrations applied.");
    }
}
