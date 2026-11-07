namespace Core.Models.Clinic;

public class AddTimeOffModel
{
    public long DoctorId { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}
