using Microsoft.AspNetCore.Authorization;

namespace Shifa.Infrastructure.Auth;

/// <summary>Requires the signed-in membership to hold a permission, e.g. [Permission(Permissions.RolesManage)].</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public PermissionAttribute(string permission) : base(PolicyPrefix + permission)
    {
    }
}

/// <summary>Requires a Platform Admin token (api/platform/... endpoints).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PlatformAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "PlatformAdmin";

    public PlatformAdminAttribute() : base(PolicyName)
    {
    }
}
