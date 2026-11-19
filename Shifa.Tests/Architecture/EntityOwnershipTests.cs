using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Core.Entities.Common;
using Infrastructure.Data;

namespace Shifa.Tests.Architecture;

/// <summary>
/// Data ownership rules from ADR-002 / tenancy-design.md, checked on the real EF models.
/// </summary>
public class EntityOwnershipTests
{
    /// <summary>Guards the tests below: an empty list would make them pass without checking anything.</summary>
    [Fact]
    public void Every_module_DbContext_is_found()
    {
        Assert.Equal(ModuleDbContexts.Expected.OrderBy(n => n), ModuleDbContexts.Types().Select(t => t.Name).OrderBy(n => n));
    }

    [Fact]
    public void Every_entity_is_tenant_owned_or_platform_owned()
    {
        var violations = new List<string>();

        foreach (var context in ModuleDbContexts.CreateAll())
        {
            using (context)
            {
                // Outbox/inbox rows are messaging infrastructure (ADR-005): no tenant filter by design, the dispatcher reads
                // every tenant's messages and each message carries its TenantId.
                foreach (var entityType in context.Model.GetEntityTypes()
                             .Where(e => !e.IsOwned() && e.ClrType != typeof(OutboxMessage) && e.ClrType != typeof(InboxMessage)))
                {
                    var clrType = entityType.ClrType;
                    var isTenantOwned = typeof(TenantOwnedEntity).IsAssignableFrom(clrType);
                    var isPlatformOwned = clrType.GetCustomAttribute<PlatformOwnedAttribute>() is not null;

                    if (isTenantOwned == isPlatformOwned)
                        violations.Add($"{clrType.Name}: must be either a TenantOwnedEntity or [PlatformOwned], not {(isTenantOwned ? "both" : "neither")}");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Tenant_owned_entities_have_tenant_filter_and_composite_key()
    {
        var violations = new List<string>();

        foreach (var context in ModuleDbContexts.CreateAll())
        {
            using (context)
            {
                foreach (var entityType in context.Model.GetEntityTypes()
                             .Where(e => typeof(TenantOwnedEntity).IsAssignableFrom(e.ClrType)))
                {
                    if (entityType.FindDeclaredQueryFilter(ModuleDbContext.TenantFilter) is null)
                        violations.Add($"{entityType.ClrType.Name}: missing '{ModuleDbContext.TenantFilter}' query filter");

                    var hasTenantKey = entityType.GetKeys().Any(key =>
                        key.Properties.Select(p => p.Name).SequenceEqual([nameof(TenantOwnedEntity.TenantId), nameof(BaseModel.Id)]));
                    if (!hasTenantKey)
                        violations.Add($"{entityType.ClrType.Name}: missing (TenantId, Id) key");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Foreign_keys_between_tenant_owned_entities_include_TenantId()
    {
        var violations = new List<string>();

        foreach (var context in ModuleDbContexts.CreateAll())
        {
            using (context)
            {
                foreach (var foreignKey in context.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
                {
                    var dependentIsTenantOwned = typeof(TenantOwnedEntity).IsAssignableFrom(foreignKey.DeclaringEntityType.ClrType);
                    var principalIsTenantOwned = typeof(TenantOwnedEntity).IsAssignableFrom(foreignKey.PrincipalEntityType.ClrType);
                    if (!dependentIsTenantOwned || !principalIsTenantOwned)
                        continue;

                    var includesTenant =
                        foreignKey.Properties.Any(p => p.Name == nameof(TenantOwnedEntity.TenantId)) &&
                        foreignKey.PrincipalKey.Properties.Any(p => p.Name == nameof(TenantOwnedEntity.TenantId));

                    if (!includesTenant)
                        violations.Add($"{foreignKey.DeclaringEntityType.ClrType.Name} -> {foreignKey.PrincipalEntityType.ClrType.Name}: " +
                                       "use HasForeignKey(c => new { c.TenantId, c.XId }).HasPrincipalKey(p => new { p.TenantId, p.Id })");
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void Each_module_uses_its_own_schema()
    {
        var violations = new List<string>();
        var contexts = ModuleDbContexts.CreateAll();

        foreach (var context in contexts)
        {
            using (context)
            {
                violations.AddRange(context.Model.GetEntityTypes()
                    .Where(e => e.GetSchema() != context.Schema)
                    .Select(e => $"{e.ClrType.Name} is in schema '{e.GetSchema()}', expected '{context.Schema}'"));
            }
        }

        violations.AddRange(contexts.GroupBy(c => c.Schema).Where(g => g.Count() > 1)
            .Select(g => $"Schema '{g.Key}' is used by more than one DbContext"));

        Assert.Empty(violations);
    }
}
