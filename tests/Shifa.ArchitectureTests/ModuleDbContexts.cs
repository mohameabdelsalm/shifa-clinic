using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Shifa.Infrastructure.Common.TenantManager;
using Shifa.Infrastructure.Data;

namespace Shifa.ArchitectureTests;

/// <summary>
/// Loads every module implementation assembly and builds each module DbContext's EF model
/// without connecting to a database, so tests can inspect entities, keys and foreign keys.
/// </summary>
internal static class ModuleDbContexts
{
    public static IReadOnlyList<Assembly> LoadModuleAssemblies() =>
        SolutionProjects.ModuleImplementations
            .Select(module => Assembly.Load(new AssemblyName(module.Name)))
            .ToList();

    public static IReadOnlyList<ModuleDbContext> CreateAll() =>
        LoadModuleAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false } && typeof(ModuleDbContext).IsAssignableFrom(type))
            .Select(Create)
            .ToList();

    private static ModuleDbContext Create(Type contextType)
    {
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;

        // Model building never opens this connection.
        builder.UseSqlServer("Server=.;Database=ModelOnly;Trusted_Connection=True");

        return (ModuleDbContext)Activator.CreateInstance(contextType, builder.Options, new TenantContext())!;
    }
}
