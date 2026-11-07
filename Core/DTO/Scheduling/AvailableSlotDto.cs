namespace Core.DTO.Scheduling;

public class AvailableSlotDto
{
    /// <summary>UTC; send this value back as StartAt to book.</summary>
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }

    /// <summary>Wall-clock time at the branch, for display.</summary>
    public DateTime LocalStart { get; set; }
}
