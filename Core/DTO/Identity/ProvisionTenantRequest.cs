namespace Core.DTO.Identity;

public sealed record ProvisionTenantRequest(long TenantId, string AdminFullName, string AdminEmail, string AdminPassword);
