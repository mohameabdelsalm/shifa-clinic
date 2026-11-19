namespace Core.DTO.Patients;

/// <summary>A row of the search list.</summary>
public class SearchPatientsDto
{
    public long Id { get; set; }
    public string Mrn { get; set; } = "";
    public string FullName { get; set; } = "";
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
