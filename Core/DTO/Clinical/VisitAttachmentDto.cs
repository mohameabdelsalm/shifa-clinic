namespace Core.DTO.Clinical;

/// <summary>Attachment metadata; the file itself is downloaded separately (audited).</summary>
public class VisitAttachmentDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public long UploadedByUserId { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}
