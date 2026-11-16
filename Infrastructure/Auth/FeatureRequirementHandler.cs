using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Auth;

/// <summary>
/// Passes when the feature is enabled for the tenant of the request (set by TenantResolutionHandler from the token).
/// Requests without a tenant (Platform Admin) never pass feature requirements. On failure the 403 response says the
/// feature isn't enabled (see AuthenticationExtensions.OnForbidden).
/// </summary>
public sealed class FeatureRequirementHandler : AuthorizationHandler<FeatureRequirement>
{
    public const string ForbiddenMessageItemKey = "Shifa.ForbiddenMessageKey";

    private readonly ITenantContext _tenant;
    private readonly ITenancyApi _tenancy;

    public FeatureRequirementHandler(ITenantContext tenant, ITenancyApi tenancy)
    {
        _tenant = tenant;
        _tenancy = tenancy;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, FeatureRequirement requirement)
    {
        if (_tenant.TenantId is long tenantId && await _tenancy.IsFeatureEnabledAsync(tenantId, requirement.FeatureCode))
        {
            context.Succeed(requirement);
            return;
        }

        if (context.Resource is HttpContext httpContext)
            httpContext.Items[ForbiddenMessageItemKey] = "FeatureNotEnabled";
    }
}
