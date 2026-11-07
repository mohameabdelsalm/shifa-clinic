using Core.Entities.Common;
using Core.Enums.Clinical;

namespace Core.Entities.Clinical;

/// <summary>Patient-level, visible to every Clinical.Read holder in the tenant for safety (ADR-010).</summary>
public class PatientAllergy : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string Allergen { get; set; } = "";
    public AllergySeverityEnum Severity { get; set; }
    public string? Reaction { get; set; }
    public long RecordedByUserId { get; set; }
}
