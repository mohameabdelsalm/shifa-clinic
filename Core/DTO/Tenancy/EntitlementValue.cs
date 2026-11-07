namespace Core.DTO.Tenancy;

/// <summary>A plan entitlement as seen by a tenant. Limit null with Enabled = true means unlimited.</summary>
public sealed record EntitlementValue(bool Enabled, int? Limit);
