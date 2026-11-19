using Core.Entities.Common;

namespace Core.Entities.Clinical;

/// <summary>A chronic condition (e.g. diabetes). Patient-level, like allergies.</summary>
public class PatientCondition : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string Condition { get; set; } = "";
    public DateOnly? Since { get; set; }
    public string? Notes { get; set; }
    public long RecordedByUserId { get; set; }
}
