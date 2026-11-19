using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth;

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

        if (policyName.StartsWith(RequireFeatureAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new FeatureRequirement(policyName[RequireFeatureAttribute.PolicyPrefix.Length..]))
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
