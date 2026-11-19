using Hangfire;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Jobs;

/// <summary>
/// Daily: removes outbox messages processed and inbox records written more than 14 days ago (delivery bookkeeping,
/// not business data). Poisoned and unprocessed outbox messages are never removed. Business data and security tokens
/// are not touched: their retention needs a decision (project/assumptions.md).
/// </summary>
public sealed class MessagingCleanupJob
{
    public static readonly TimeSpan Keep = TimeSpan.FromDays(14);

    private readonly IServiceProvider _services;
    private readonly OutboxSources _outboxSources;
    private readonly TimeProvider _time;
    private readonly ILogger<MessagingCleanupJob> _logger;

    public MessagingCleanupJob(IServiceProvider services, OutboxSources outboxSources, TimeProvider time, ILogger<MessagingCleanupJob> logger)
    {
        _services = services;
        _outboxSources = outboxSources;
        _time = time;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 1800)]
    [AutomaticRetry(Attempts = 0)]
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var cutoff = _time.GetUtcNow() - Keep;
        var removed = 0;

        foreach (var contextType in _outboxSources.ContextTypes)
        {
            var context = (ModuleDbContext)_services.GetRequiredService(contextType);
            removed += await context.OutboxMessages.Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoff).ExecuteDeleteAsync(cancellationToken);
        }

        var notifications = _services.GetRequiredService<NotificationsDbContext>();
        removed += await notifications.InboxMessages.Where(m => m.ProcessedAt < cutoff).ExecuteDeleteAsync(cancellationToken);

        if (removed > 0)
            _logger.LogInformation("Messaging cleanup removed {Count} processed outbox/inbox rows", removed);
        return removed;
    }
}
