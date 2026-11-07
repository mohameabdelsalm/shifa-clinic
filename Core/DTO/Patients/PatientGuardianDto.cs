namespace Core.DTO.Patients;

public class PatientGuardianDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
