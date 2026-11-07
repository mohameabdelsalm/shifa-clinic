namespace Shifa.Core.Entities.Common;

/// <summary>
/// Marks an entity that belongs to the platform, not to a tenant (e.g. Tenant, Plan, User).
/// Every entity must be either a <see cref="TenantOwnedEntity"/> or marked with this attribute;
/// Shifa.ArchitectureTests fails otherwise.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PlatformOwnedAttribute : Attribute
{
}
