namespace Core.DTO.Scheduling;

public class AppointmentDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }
    public long PatientId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateOnly LocalDate { get; set; }
    public string Status { get; set; } = "";
    public string Source { get; set; } = "";
    public int? QueueNumber { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
}
