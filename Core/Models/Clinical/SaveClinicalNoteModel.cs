namespace Core.Models.Clinical;

public class SaveClinicalNoteModel
{
    public long VisitId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Notes { get; set; }
}
