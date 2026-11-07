namespace Core.Models.Scheduling;

public class AvailabilityModel
{
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }

    /// <summary>The day in the branch's local calendar.</summary>
    public DateOnly Date { get; set; }
}
