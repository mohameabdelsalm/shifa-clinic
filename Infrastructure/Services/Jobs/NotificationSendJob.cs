using Core.Enums.Notifications;
using Core.Interfaces.Common;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Jobs;

/// <summary>Every minute: sends due notifications (confirmations, reminders, receipts), each clinic in its own scope.</summary>
public sealed class NotificationSendJob
{
    private const int BatchSize = 200;

    private readonly NotificationsDbContext _context;
    private readonly ITenantScopeFactory _scopes;
    private readonly TimeProvider _time;

    public NotificationSendJob(NotificationsDbContext context, ITenantScopeFactory scopes, TimeProvider time)
    {
        _context = context;
        _scopes = scopes;
        _time = time;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 600)]
    [AutomaticRetry(Attempts = 0)]
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        var due = await _context.Notifications.IgnoreQueryFilters([ModuleDbContext.TenantFilter]).AsNoTracking()
            .Where(n => n.Status == NotificationStatusEnum.Scheduled && n.ScheduledAt <= now && !n.IsDeleted)
            .OrderBy(n => n.ScheduledAt)
            .Take(BatchSize)
            .Select(n => new { n.Id, n.TenantId })
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var tenant in due.GroupBy(n => n.TenantId))
        {
            var ids = tenant.Select(n => n.Id).ToList();
            sent += await _scopes.RunAsync(tenant.Key, sp => sp.GetRequiredService<NotificationSender>().SendDueAsync(ids, cancellationToken));
        }
        return sent;
    }
}
