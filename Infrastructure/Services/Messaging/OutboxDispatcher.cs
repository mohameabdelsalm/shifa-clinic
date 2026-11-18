using Core.Entities.Common;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Messaging;

/// <summary>
/// Polls every outbox (ADR-005, ADR-015). Messages are claimed with a lease (UPDLOCK + READPAST), so several API
/// instances can run the dispatcher without handling the same message at the same time. Failures are retried with
/// exponential backoff; after <see cref="OutboxOptions.MaxAttempts"/> the message is poisoned and logged. Logs carry the
/// message id and type only, never the payload.
/// </summary>
public sealed class OutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSources _sources;
    private readonly OutboxOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, OutboxSources sources, IOptions<OutboxOptions> options, TimeProvider time,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _sources = sources;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            var handled = 0;
            foreach (var contextType in _sources.ContextTypes)
            {
                try
                {
                    handled += await DispatchBatchAsync(contextType, stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Outbox polling of {Context} failed", contextType.Name);
                }
            }

            if (handled == 0)
            {
                try
                {
                    await Task.Delay(_options.PollInterval, _time, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    /// <summary>Claims and handles one batch of one module's outbox; returns how many messages it handled.</summary>
    public async Task<int> DispatchBatchAsync(Type contextType, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = (ModuleDbContext)scope.ServiceProvider.GetRequiredService(contextType);
        var dispatcher = scope.ServiceProvider.GetRequiredService<IntegrationEventDispatcher>();

        var now = _time.GetUtcNow();
        var ids = await ClaimAsync(context, now, cancellationToken);
        if (ids.Count == 0)
            return 0;

        var messages = await context.OutboxMessages.Where(m => ids.Contains(m.Id)).OrderBy(m => m.OccurredAt).ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                await dispatcher.DispatchAsync(message, cancellationToken);
                message.ProcessedAt = _time.GetUtcNow();
                message.LastError = null;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                RecordFailure(message, ex);
            }

            message.LockedUntil = null;
            await context.SaveChangesAsync(CancellationToken.None);
        }

        return messages.Count;
    }

    private async Task<List<Guid>> ClaimAsync(ModuleDbContext context, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Schema names are constants of the module DbContexts, not input.
        var sql = $"""
            WITH batch AS (
                SELECT TOP (@size) [Id], [LockedUntil]
                FROM [{context.Schema}].[OutboxMessages] WITH (ROWLOCK, UPDLOCK, READPAST)
                WHERE [ProcessedAt] IS NULL AND [PoisonedAt] IS NULL AND [NextAttemptAt] <= @now
                  AND ([LockedUntil] IS NULL OR [LockedUntil] < @now)
                ORDER BY [OccurredAt])
            UPDATE batch SET [LockedUntil] = @lockedUntil
            OUTPUT inserted.[Id] AS [Value];
            """;

        return await context.Database.SqlQueryRaw<Guid>(sql,
                new SqlParameter("@size", _options.BatchSize),
                new SqlParameter("@now", now),
                new SqlParameter("@lockedUntil", now + _options.Lease))
            .ToListAsync(cancellationToken);
    }

    private void RecordFailure(OutboxMessage message, Exception ex)
    {
        var now = _time.GetUtcNow();
        message.Attempts++;
        var error = $"{ex.GetType().Name}: {ex.Message}";
        message.LastError = error.Length > 1000 ? error[..1000] : error;

        if (ex is UnknownEventTypeException || message.Attempts >= _options.MaxAttempts)
        {
            message.PoisonedAt = now;
            _logger.LogError(ex, "Outbox message {MessageId} ({Type}) poisoned after {Attempts} attempts", message.Id, message.Type, message.Attempts);
            return;
        }

        var backoff = TimeSpan.FromSeconds(Math.Pow(2, message.Attempts) * 5);
        message.NextAttemptAt = now + (backoff < MaxBackoff ? backoff : MaxBackoff);
        _logger.LogWarning(ex, "Outbox message {MessageId} ({Type}) failed, attempt {Attempts}; retry at {NextAttemptAt}",
            message.Id, message.Type, message.Attempts, message.NextAttemptAt);
    }
}
