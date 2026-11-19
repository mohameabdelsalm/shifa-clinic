namespace Core.DTO.Billing;

public class RefundDto
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public string Number { get; set; } = "";
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";

    /// <summary>Completed, Pending (online, waiting for the gateway) or Failed.</summary>
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public long RefundedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
