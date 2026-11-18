namespace Core.Events;

/// <summary>
/// An integration event written to the publishing module's outbox in the same save as the change (ADR-005, ADR-015).
/// The class name is the message type, so never rename an event class: add a new one instead.
/// Events carry ids and times only: never medical details or contact data.
/// </summary>
public interface IIntegrationEvent
{
}
