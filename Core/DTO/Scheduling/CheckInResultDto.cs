namespace Core.DTO.Scheduling;

/// <summary>Result of a check-in: the queue number and the visit invoice to collect.</summary>
public class CheckInResultDto
{
    public int? QueueNumber { get; set; }
    /// <summary>Null when the clinic doesn't use the Billing feature.</summary>
    public long? InvoiceId { get; set; }
}
