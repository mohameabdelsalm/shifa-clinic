namespace Core.DTO.Patients;

/// <summary>What a portal user sees of a patient they act for: no identifiers, no internal counters.</summary>
public class PortalPatientDto
{
    public long Id { get; set; }
    public string Mrn { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string Relationship { get; set; } = "";
    public bool CanBook { get; set; }
}
