namespace Core.Events;

/// <summary>
/// Handles one event type. Runs in its own scope with the event's tenant set. Delivery is at-least-once: handlers
/// must be idempotent (derive from InboxEventHandler, which records the message id in the module's inbox).
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, Guid messageId, CancellationToken cancellationToken);
}
