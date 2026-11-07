using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Shifa.Core.Interfaces.Common;

namespace Shifa.Infrastructure.Auth;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;
    public string Permission { get; }
}

public sealed class PlatformAdminRequirement : IAuthorizationRequirement
{
}

public sealed class PortalUserRequirement : IAuthorizationRequirement
{
}

/// <summary>Used by the default and fallback policies: any signed-in caller except a patient portal user.</summary>
public sealed class NotPortalUserRequirement : IAuthorizationRequirement
{
}

/// <summary>Builds the "Permission:X", "PlatformAdmin" and "PortalUser" policies on demand (same idea as the Trio Auth API).</summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName == PlatformAdminAttribute.PolicyName)
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PlatformAdminRequirement())
                .Build();
        }

        if (policyName == PortalUserAttribute.PolicyName)
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PortalUserRequirement())
                .Build();
        }

        if (policyName.StartsWith(PermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[PermissionAttribute.PolicyPrefix.Length..]))
                .Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}

/// <summary>
/// Decides from IAccessContext, which TenantResolutionHandler filled from a validated token.
/// Platform, staff and portal authority never mix: a Platform Admin has no tenant permissions,
/// and a portal user has none either (only the portal endpoints).
/// </summary>
public sealed class AccessRequirementHandler : IAuthorizationHandler
{
    private readonly IAccessContext _access;

    public AccessRequirementHandler(IAccessContext access)
    {
        _access = access;
    }

    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
        {
            var allowed = requirement switch
            {
                PermissionRequirement p => !_access.IsPlatformAdmin && !_access.IsPortalUser && _access.HasPermission(p.Permission),
                PlatformAdminRequirement => _access.IsPlatformAdmin,
                PortalUserRequirement => _access.IsPortalUser,
                NotPortalUserRequirement => !_access.IsPortalUser,
                _ => false,
            };

            if (allowed)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
