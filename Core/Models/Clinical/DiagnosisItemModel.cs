namespace Core.Models.Clinical;

public class DiagnosisItemModel
{
    public string Icd10Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
}
