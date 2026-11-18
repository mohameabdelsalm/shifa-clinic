namespace Infrastructure.Services.Messaging;

/// <summary>An outbox message whose type no event class has (renamed or removed): poisoned at once.</summary>
public sealed class UnknownEventTypeException : Exception
{
    public UnknownEventTypeException(string type) : base($"Unknown integration event type '{type}'.")
    {
    }
}
