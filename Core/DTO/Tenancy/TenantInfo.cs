namespace Core.DTO.Tenancy;

/// <param name="IsActive">Staff and patients may sign in: Trial/Active tenant, and the subscription ended less than 30 days ago (A6).</param>
/// <param name="IsReadOnly">The subscription ended (within the 30-day grace): reads only, no changes (A6, ADR-014).</param>
public sealed record TenantInfo(long Id, string Slug, string Name, string Status, bool IsActive, string CurrencyCode = "EGP", bool IsReadOnly = false);
