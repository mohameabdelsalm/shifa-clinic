namespace Core.DTO.Clinical;

/// <summary>The full visit record (reading it is audited).</summary>
public class VisitDto : VisitListDto
{
    public ClinicalNoteDto? Note { get; set; }
    public List<VitalSignDto> Vitals { get; set; } = [];
    public List<DiagnosisDto> Diagnoses { get; set; } = [];
    public PrescriptionDto? Prescription { get; set; }
    public List<VisitAttachmentDto> Attachments { get; set; } = [];
    public List<ClinicalAddendumDto> Addenda { get; set; } = [];
}
