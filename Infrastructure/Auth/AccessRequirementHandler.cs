using Core.Interfaces.Common;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

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
