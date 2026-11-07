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

/// <summary>Builds the "Permission:X" and "PlatformAdmin" policies on demand (same idea as the Trio Auth API).</summary>
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
/// Decides from IAccessContext, which TenantResolutionHandler filled from a validated membership.
/// A Platform Admin has no tenant permissions: platform and tenant authority never mix.
/// </summary>
public sealed class AccessRequirementHandler :
    IAuthorizationHandler
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
                PermissionRequirement p => !_access.IsPlatformAdmin && _access.HasPermission(p.Permission),
                PlatformAdminRequirement => _access.IsPlatformAdmin,
                _ => false,
            };

            if (allowed)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
