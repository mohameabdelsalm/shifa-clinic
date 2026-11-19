namespace Core.Models.Clinical;

public class AddAddendumModel
{
    /// <summary>A completed visit.</summary>
    public long VisitId { get; set; }
    public string Reason { get; set; } = "";
    public string Content { get; set; } = "";
}
