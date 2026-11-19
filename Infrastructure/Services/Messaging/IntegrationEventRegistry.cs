using Core.Events;

namespace Infrastructure.Services.Messaging;

/// <summary>Event type names → CLR types, and each event type's handler classes (filled at startup by AddIntegrationEventHandler).</summary>
public sealed class IntegrationEventRegistry
{
    private readonly Dictionary<string, Type> _eventTypes = typeof(IIntegrationEvent).Assembly.GetTypes()
        .Where(t => typeof(IIntegrationEvent).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
        .ToDictionary(t => t.Name, StringComparer.Ordinal);

    private readonly Dictionary<Type, List<Type>> _handlers = [];

    public IntegrationEventRegistry(IEnumerable<HandlerRegistration> registrations)
    {
        foreach (var registration in registrations)
            AddHandler(registration.EventType, registration.HandlerType);
    }

    public Type? FindEventType(string name) => _eventTypes.GetValueOrDefault(name);

    public IReadOnlyList<Type> HandlersOf(Type eventType) => _handlers.TryGetValue(eventType, out var list) ? list : [];

    private void AddHandler(Type eventType, Type handlerType)
    {
        if (!_handlers.TryGetValue(eventType, out var list))
            _handlers[eventType] = list = [];
        if (!list.Contains(handlerType))
            list.Add(handlerType);
    }
}
