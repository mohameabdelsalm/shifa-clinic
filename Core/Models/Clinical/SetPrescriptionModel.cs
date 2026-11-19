namespace Core.Models.Clinical;

/// <summary>Replaces the visit's prescription (no items removes it).</summary>
public class SetPrescriptionModel
{
    public long VisitId { get; set; }
    public string? Notes { get; set; }
    public List<PrescriptionItemModel> Items { get; set; } = [];
}
