namespace Core.Models.Patients;

public class PatientGuardianModel
{
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
