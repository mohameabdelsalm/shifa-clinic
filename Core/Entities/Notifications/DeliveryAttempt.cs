using Core.Entities.Common;

namespace Core.Entities.Notifications;

/// <summary>One try to hand a notification to the SMS/e-mail provider.</summary>
public class DeliveryAttempt : TenantOwnedEntity
{
    public long NotificationId { get; set; }
    public string Provider { get; set; } = "";
    public bool Succeeded { get; set; }

    /// <summary>The exception type only (provider messages can echo the recipient).</summary>
    public string? Error { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}
