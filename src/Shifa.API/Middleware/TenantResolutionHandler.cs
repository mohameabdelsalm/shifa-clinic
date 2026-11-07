using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Shifa.Core;
using Shifa.Core.Common;
using Shifa.Core.Interfaces.Common;
using Shifa.Identity.Contracts;
using Shifa.Tenancy.Contracts;

namespace Shifa.API.Middleware;

/// <summary>
/// Sets ITenantContext and IAccessContext for the request from the authenticated token (ADR-002, ADR-007).
/// The tenant never comes from the request body, query string or headers. Before trusting the token's tenant
/// it checks (cached for 30 seconds) that the tenant is active and that the membership is still active
/// and belongs to the token's user. Anything else is answered with 401 and the generic session message.
/// A Platform Admin token has no tenant; the user is re-checked as an active Platform Admin.
/// </summary>
public sealed class TenantResolutionHandler
{
    private readonly RequestDelegate _next;

    public TenantResolutionHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IAccessContext accessContext,
        IIdentityApi identity, ITenancyApi tenancy, IStringLocalizer<LanguageResource> localizer)
    {
        // Sign-in, refresh and sign-out must stay reachable with a stale token (e.g. of a suspended tenant);
        // they never read tenant data before authenticating the caller themselves.
        var allowsAnonymous = context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

        if (context.User.Identity?.IsAuthenticated != true || allowsAnonymous)
        {
            await _next(context);
            return;
        }

        var cancellationToken = context.RequestAborted;
        var userId = ReadId(context.User, ShifaClaimTypes.UserId);
        var isAuthorized = false;

        if (userId is not null)
        {
            if (context.User.HasClaim(ShifaClaimTypes.PlatformAdmin, "true"))
            {
                if (!context.User.HasClaim(c => c.Type == ShifaClaimTypes.TenantId) &&
                    await identity.IsActivePlatformAdminAsync(userId.Value, cancellationToken))
                {
                    accessContext.SetPlatformAdmin();
                    isAuthorized = true;
                }
            }
            else
            {
                var tenantId = ReadId(context.User, ShifaClaimTypes.TenantId);
                var membershipId = ReadId(context.User, ShifaClaimTypes.MembershipId);

                if (tenantId is not null && membershipId is not null)
                {
                    var tenant = await tenancy.GetTenantAsync(tenantId.Value, cancellationToken);
                    var access = tenant is { IsActive: true }
                        ? await identity.GetMembershipAccessAsync(tenantId.Value, membershipId.Value, userId.Value, cancellationToken)
                        : null;

                    if (access is not null)
                    {
                        tenantContext.SetTenant(tenantId.Value);
                        accessContext.SetMembership(access.MembershipId, access.AllBranches, access.BranchIds, access.Permissions);
                        isAuthorized = true;
                    }
                }
            }
        }

        if (!isAuthorized)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(
                new Response(localizer["InvalidToken"].Value, HttpStatusCode.Unauthorized, isSuccess: false), cancellationToken);
            return;
        }

        await _next(context);
    }

    private static long? ReadId(ClaimsPrincipal user, string claimType) =>
        long.TryParse(user.FindFirstValue(claimType), out var id) && id > 0 ? id : null;
}
