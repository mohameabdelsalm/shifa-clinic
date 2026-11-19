namespace Core.DTO.Clinical;

/// <summary>
/// The patient's clinical overview: allergies and conditions (whole tenant, for safety) and the latest visits in the
/// reader's branches (ADR-010). Reading it is audited.
/// </summary>
public class PatientClinicalSummaryDto
{
    public long PatientId { get; set; }
    public List<PatientAllergyDto> Allergies { get; set; } = [];
    public List<PatientConditionDto> Conditions { get; set; } = [];
    public List<VisitListDto> RecentVisits { get; set; } = [];
}
