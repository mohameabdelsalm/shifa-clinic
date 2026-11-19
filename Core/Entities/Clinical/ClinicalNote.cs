using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>The doctor's note for a visit (one per visit).</summary>
public class ClinicalNote : TenantOwnedEntity
{
    public long VisitId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Notes { get; set; }
}
