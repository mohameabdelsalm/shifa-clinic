namespace Core.DTO.Clinical;

public class ClinicalAddendumDto
{
    public long Id { get; set; }
    public long AuthorUserId { get; set; }
    public string Reason { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}
