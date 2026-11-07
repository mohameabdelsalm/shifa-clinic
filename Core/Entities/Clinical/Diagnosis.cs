using Core.Entities.Common;

namespace Core.Entities.Clinical;

public class Diagnosis : TenantOwnedEntity
{
    public long VisitId { get; set; }

    /// <summary>ICD-10 code, e.g. "J06.9" (format checked; no code catalog yet).</summary>
    public string Icd10Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsPrimary { get; set; }
}
