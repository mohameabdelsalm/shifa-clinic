using Core.Entities.Identity;
using Core.Interfaces.Common;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services.Identity;

/// <summary>Writes append-only rows to identity.AuditLogs. Audit rows are never updated or deleted.</summary>
public sealed class AuditService : IAuditService
{
    private readonly IdentityDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _http;

    public AuditService(IdentityDbContext context, ICurrentUser currentUser, ITenantContext tenantContext, IHttpContextAccessor http)
    {
        _context = context;
        _currentUser = currentUser;
        _tenantContext = tenantContext;
        _http = http;
    }

    public async Task LogAsync(string action, string? entity = null, long? entityId = null, long? targetTenantId = null,
        string? detail = null, long? userId = null, CancellationToken cancellationToken = default)
    {
        await _context.AuditLogs.AddAsync(new AuditLog
        {
            UserId = userId ?? _currentUser.GetUserId(),
            TenantId = _tenantContext.TenantId,
            TargetTenantId = targetTenantId ?? _tenantContext.TenantId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Detail = detail,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
