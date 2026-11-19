namespace Core.DTO.Clinical;

public class PatientAllergyDto
{
    public long Id { get; set; }
    public string Allergen { get; set; } = "";
    public string Severity { get; set; } = "";
    public string? Reaction { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
