namespace Core.Models.Clinical;

public class PrescriptionItemModel
{
    public string DrugName { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}
