namespace Core.DTO.Patients;

public class PatientPortalAccessDto
{
    public long Id { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string Relationship { get; set; } = "";
    public bool CanBook { get; set; }
}
