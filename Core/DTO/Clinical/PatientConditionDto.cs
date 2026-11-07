namespace Core.DTO.Clinical;

public class PatientConditionDto
{
    public long Id { get; set; }
    public string Condition { get; set; } = "";
    public DateOnly? Since { get; set; }
    public string? Notes { get; set; }
}
