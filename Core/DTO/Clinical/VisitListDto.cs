namespace Core.DTO.Clinical;

/// <summary>A visit in a list: who, where, when and state, without clinical content.</summary>
public class VisitListDto
{
    public long Id { get; set; }
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
