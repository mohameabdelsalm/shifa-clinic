namespace Core.DTO.Clinic;

public class DoctorTimeOffDto
{
    public long Id { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}
