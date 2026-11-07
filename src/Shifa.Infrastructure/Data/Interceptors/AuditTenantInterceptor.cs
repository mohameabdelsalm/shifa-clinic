using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shifa.Core.Entities.Common;
using Shifa.Core.Interfaces.Common;

namespace Shifa.Infrastructure.Data.Interceptors;

/// <summary>
/// Runs before every SaveChanges of every module DbContext:
/// <list type="number">
/// <item>Tenant guard (ADR-002): stamps TenantId on new tenant-owned rows and throws when a save
/// would insert, update or delete a row of another tenant, change a row's TenantId, or write
/// tenant-owned data without a tenant context.</item>
/// <item>Audit columns: CreationDate/CreatedBy and ModificationDate/ModifiedBy.</item>
/// <item>Soft delete: a Remove becomes IsDeleted = true. Rows are never physically deleted.</item>
/// </list>
/// A guard violation is a bug or an attack, not a business error, so it throws
/// InvalidOperationException (logged, returned to the client as a generic 500).
/// </summary>
public sealed class AuditTenantInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditTenantInterceptor(ITenantContext tenantContext, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var now = _timeProvider.GetUtcNow();
        var userId = _currentUser.GetUserId();

        foreach (var entry in context.ChangeTracker.Entries<BaseModel>())
        {
            if (entry.Entity is TenantOwnedEntity)
                GuardTenant(entry);

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreationDate = now;
                    entry.Entity.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModificationDate = now;
                    entry.Entity.ModifiedBy = userId;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.ModificationDate = now;
                    entry.Entity.ModifiedBy = userId;
                    break;
            }
        }
    }

    private void GuardTenant(EntityEntry<BaseModel> entry)
    {
        if (entry.State is EntityState.Unchanged or EntityState.Detached)
            return;

        var entityName = entry.Metadata.ClrType.Name;
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException($"Cannot save {entityName}: there is no tenant context.");

        var tenantProperty = entry.Property(nameof(TenantOwnedEntity.TenantId));

        if (entry.State == EntityState.Added)
        {
            var assigned = (long)tenantProperty.CurrentValue!;
            if (assigned == 0)
                tenantProperty.CurrentValue = tenantId;
            else if (assigned != tenantId)
                throw new InvalidOperationException($"Cannot insert {entityName} for another tenant.");
            return;
        }

        var original = (long)tenantProperty.OriginalValue!;
        var current = (long)tenantProperty.CurrentValue!;

        if (original != tenantId)
            throw new InvalidOperationException($"Cannot modify {entityName} of another tenant.");

        if (current != original)
            throw new InvalidOperationException($"Cannot change the tenant of {entityName}.");
    }
}
