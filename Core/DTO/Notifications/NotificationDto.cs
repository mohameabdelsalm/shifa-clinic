namespace Core.DTO.Notifications;

/// <summary>What was (or will be) sent to a patient. The recipient is masked; the text itself isn't returned.</summary>
public class NotificationDto
{
    public long Id { get; set; }
    public string Kind { get; set; } = "";
    public string Channel { get; set; } = "";
    public string Recipient { get; set; } = "";
    public string Language { get; set; } = "";
    public long? PatientId { get; set; }
    public long? BranchId { get; set; }
    public long? AppointmentId { get; set; }
    public string Status { get; set; } = "";
    public string? StatusReason { get; set; }
    public DateTimeOffset ScheduledAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
}
