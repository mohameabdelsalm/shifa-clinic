using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>The prescription of a visit (at most one); its drugs are <see cref="PrescriptionItem"/> rows.</summary>
public class Prescription : TenantOwnedEntity
{
    public long VisitId { get; set; }
    public string? Notes { get; set; }
}
