namespace Infrastructure.Services.Messaging;

/// <summary>One "event type → handler class" pair, collected into <see cref="IntegrationEventRegistry"/>.</summary>
public sealed record HandlerRegistration(Type EventType, Type HandlerType);
