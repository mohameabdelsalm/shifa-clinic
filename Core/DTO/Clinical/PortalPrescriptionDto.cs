namespace Core.DTO.Clinical;

/// <summary>A prescription as the patient sees it in the portal: drugs and instructions only.</summary>
public class PortalPrescriptionDto
{
    public long VisitId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionItemDto> Items { get; set; } = [];
}
