namespace Core.DTO.Scheduling;

/// <summary>
/// What a patient or guardian sees about an appointment in the portal. Separate from the staff
/// <see cref="AppointmentDto"/>: no booking source, check-in/cancel timestamps or staff-written cancel reasons.
/// </summary>
public class PortalAppointmentDto
{
    public long Id { get; set; }
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public DateOnly LocalDate { get; set; }
    public string Status { get; set; } = "";

    /// <summary>The patient's own place in the queue after check-in.</summary>
    public int? QueueNumber { get; set; }
}
