namespace Core.Models.Clinical;

public class AddConditionModel
{
    public long PatientId { get; set; }
    public string Condition { get; set; } = "";
    public DateOnly? Since { get; set; }
    public string? Notes { get; set; }
}
