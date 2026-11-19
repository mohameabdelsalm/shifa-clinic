using Microsoft.EntityFrameworkCore;
using Infrastructure.Common.TenantManager;
using Infrastructure.Data;

namespace Shifa.Tests.Architecture;

/// <summary>
/// Every module DbContext of the Infrastructure assembly, with its EF model built without connecting to a database,
/// so tests can inspect entities, keys and foreign keys.
/// </summary>
internal static class ModuleDbContexts
{
    /// <summary>One per module (ADR-001/003). A new module must be added here on purpose.</summary>
    public static readonly string[] Expected =
    [
        nameof(TenancyDbContext), nameof(IdentityDbContext), nameof(ClinicDbContext), nameof(PatientsDbContext),
        nameof(SchedulingDbContext), nameof(ClinicalDbContext), nameof(BillingDbContext), nameof(NotificationsDbContext),
    ];

    public static IReadOnlyList<Type> Types() =>
        typeof(ModuleDbContext).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false } && typeof(ModuleDbContext).IsAssignableFrom(type))
            .ToList();

    public static IReadOnlyList<ModuleDbContext> CreateAll() => Types().Select(Create).ToList();

    private static ModuleDbContext Create(Type contextType)
    {
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;

        // Model building never opens this connection.
        builder.UseSqlServer("Server=.;Database=ModelOnly;Trusted_Connection=True");

        return (ModuleDbContext)Activator.CreateInstance(contextType, builder.Options, new TenantContext())!;
    }
}
