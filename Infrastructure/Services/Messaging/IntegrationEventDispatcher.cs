using System.Text.Json;
using Core.Entities.Common;
using Core.Events;
using Core.Interfaces.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Messaging;

/// <summary>
/// Delivers one outbox message to every handler of its type, each in its own scope with the message's tenant set.
/// All handlers are tried; if any failed the whole message is retried later (handlers that succeeded skip it through
/// their inbox).
/// </summary>
public sealed class IntegrationEventDispatcher
{
    private readonly IntegrationEventRegistry _registry;
    private readonly ITenantScopeFactory _scopes;

    public IntegrationEventDispatcher(IntegrationEventRegistry registry, ITenantScopeFactory scopes)
    {
        _registry = registry;
        _scopes = scopes;
    }

    public async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var eventType = _registry.FindEventType(message.Type)
            ?? throw new UnknownEventTypeException(message.Type);
        var integrationEvent = JsonSerializer.Deserialize(message.PayloadJson, eventType)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has an empty payload.");
        var tenantId = message.TenantId ?? throw new InvalidOperationException($"Outbox message {message.Id} has no tenant.");

        var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handle = handlerInterface.GetMethod(nameof(IIntegrationEventHandler<IIntegrationEvent>.HandleAsync))!;

        var failures = new List<Exception>();
        foreach (var handlerType in _registry.HandlersOf(eventType))
        {
            try
            {
                await _scopes.RunAsync(tenantId, async sp =>
                {
                    var handler = sp.GetRequiredService(handlerType);
                    await (Task)handle.Invoke(handler, [integrationEvent, message.Id, cancellationToken])!;
                    return true;
                });
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                failures.Add(ex);
            }
        }

        if (failures.Count == 1)
            throw failures[0];
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
