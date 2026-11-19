namespace Core.Models.Scheduling;

public class PortalBookAppointmentModel
{
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }
    public DateTimeOffset StartAt { get; set; }
}
