namespace Core.DTO.Clinical;

public class DiagnosisDto
{
    public long Id { get; set; }
    public string Icd10Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
}
