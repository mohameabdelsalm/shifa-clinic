namespace Core.DTO.Patients;

public class PatientConsentDto
{
    public long Id { get; set; }
    public string ConsentType { get; set; } = "";
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
