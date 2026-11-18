using Core.Entities.Common;
using Core.Events;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Messaging;

/// <summary>
/// Base of every event handler (ADR-005): in one transaction of the consumer's module, skips a message it already
/// handled, runs <see cref="HandleCoreAsync"/> and records the message id in the inbox. <see cref="HandleCoreAsync"/>
/// only adds/changes tracked entities; this class saves once.
/// </summary>
public abstract class InboxEventHandler<TEvent, TContext> : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
    where TContext : ModuleDbContext
{
    protected InboxEventHandler(TContext context, TimeProvider time)
    {
        Context = context;
        Time = time;
    }

    protected TContext Context { get; }
    protected TimeProvider Time { get; }

    public async Task HandleAsync(TEvent integrationEvent, Guid messageId, CancellationToken cancellationToken)
    {
        var consumer = GetType().Name;
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        if (await Context.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.Consumer == consumer, cancellationToken))
            return;

        await HandleCoreAsync(integrationEvent, cancellationToken);
        Context.InboxMessages.Add(new InboxMessage { MessageId = messageId, Consumer = consumer, ProcessedAt = Time.GetUtcNow() });
        await Context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    protected abstract Task HandleCoreAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
