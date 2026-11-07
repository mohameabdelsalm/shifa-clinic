namespace Core.DTO.Patients;

/// <summary>The full profile, for staff with Patients.Read.</summary>
public class PatientDto : SearchPatientsDto
{
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int NoShowCount { get; set; }
    public List<PatientIdentifierDto> Identifiers { get; set; } = [];
    public List<PatientGuardianDto> Guardians { get; set; } = [];
    public List<PatientConsentDto> Consents { get; set; } = [];
    public List<PatientPortalAccessDto> PortalAccess { get; set; } = [];
}
