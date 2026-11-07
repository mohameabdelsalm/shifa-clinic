using System.Security.Claims;
using Shifa.Core.Common;
using Shifa.Core.Interfaces.Common;

namespace Shifa.API.Middleware;

/// <summary>
/// Sets ITenantContext for the request from the authenticated token's tenant claim (ADR-002, ADR-007).
/// The tenant never comes from the request body, query string or headers.
/// <para>
/// Phase 2 adds: validate that the membership and the tenant are active (cached) before setting it,
/// and reject the request when they aren't.
/// </para>
/// </summary>
public sealed class TenantResolutionHandler
{
    private readonly RequestDelegate _next;

    public TenantResolutionHandler(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && long.TryParse(context.User.FindFirstValue(ShifaClaimTypes.TenantId), out var tenantId))
        {
            tenantContext.SetTenant(tenantId);
        }

        await _next(context);
    }
}
