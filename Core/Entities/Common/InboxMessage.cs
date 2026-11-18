namespace Core.Entities.Common;

/// <summary>A message a consumer has handled (ADR-005): makes at-least-once delivery run each handler once.</summary>
public class InboxMessage
{
    public Guid MessageId { get; set; }

    /// <summary>The handler class name.</summary>
    public string Consumer { get; set; } = "";
    public DateTimeOffset ProcessedAt { get; set; }
}
