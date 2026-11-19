namespace Core.Models.Clinical;

/// <summary>Replaces the visit's diagnoses (an empty list removes them).</summary>
public class SetDiagnosesModel
{
    public long VisitId { get; set; }
    public List<DiagnosisItemModel> Items { get; set; } = [];
}
