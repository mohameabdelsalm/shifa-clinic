namespace Core.DTO.Tenancy;

public sealed record TenantInfo(long Id, string Slug, string Name, string Status, bool IsActive);
