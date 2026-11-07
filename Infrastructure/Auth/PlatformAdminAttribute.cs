using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>Requires a Platform Admin token (api/platform/... endpoints).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PlatformAdminAttribute : AuthorizeAttribute
{
    public const string PolicyName = "PlatformAdmin";

    public PlatformAdminAttribute() : base(PolicyName)
    {
    }
}
