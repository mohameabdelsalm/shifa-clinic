namespace Core.Models.Patients;

public class AddGuardianModel
{
    public long PatientId { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
