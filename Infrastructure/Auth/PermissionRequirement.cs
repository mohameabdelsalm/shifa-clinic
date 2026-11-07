using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}
