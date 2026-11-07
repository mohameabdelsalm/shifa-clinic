namespace Shifa.Core.Entities.Common;

/// <summary>
/// Base of every entity that belongs to one tenant (ADR-002).
/// <para>
/// <see cref="TenantId"/> has a private setter on purpose: application code can't assign it.
/// AuditTenantInterceptor stamps it from ITenantContext when the entity is inserted and
/// rejects saves that would touch another tenant's row.
/// </para>
/// </summary>
public abstract class TenantOwnedEntity : BaseModel
{
    public long TenantId { get; private set; }
}
