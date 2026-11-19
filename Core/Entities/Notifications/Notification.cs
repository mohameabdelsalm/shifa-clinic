using Core.Entities.Common;
using Core.Enums.Notifications;

namespace Core.Entities.Notifications;

/// <summary>
/// One message to a patient (ADR-015). Created by the event handlers, sent by the notifications job when due.
/// The payload holds only what the template shows (names, date/time, amounts); never medical details.
/// </summary>
public class Notification : TenantOwnedEntity
{
    public const int MaxAttempts = 3;

    public NotificationKindEnum Kind { get; set; }
    public NotificationChannelEnum Channel { get; set; }

    /// <summary>Phone number (E.164) or e-mail address.</summary>
    public string Recipient { get; set; } = "";
    public string Language { get; set; } = "ar";

    /// <summary>Template values as a JSON object of strings.</summary>
    public string PayloadJson { get; set; } = "{}";

    public long? PatientId { get; set; }
    public long? BranchId { get; set; }
    public long? AppointmentId { get; set; }

    /// <summary>The appointment start this message was planned for: if the appointment moved, the message is cancelled.</summary>
    public DateTimeOffset? AppointmentStartAt { get; set; }

    public NotificationStatusEnum Status { get; set; } = NotificationStatusEnum.Scheduled;
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }

    /// <summary>Why it was cancelled/skipped/failed (a short code, no personal data).</summary>
    public string? StatusReason { get; set; }
}
