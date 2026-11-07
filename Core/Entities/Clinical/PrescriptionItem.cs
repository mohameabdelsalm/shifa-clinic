using Core.Entities.Common;

namespace Core.Entities.Clinical;

public class PrescriptionItem : TenantOwnedEntity
{
    public long PrescriptionId { get; set; }
    public string DrugName { get; set; } = "";
    public string Dosage { get; set; } = "";
    public string Frequency { get; set; } = "";
    public int DurationDays { get; set; }
    public string? Instructions { get; set; }
}
