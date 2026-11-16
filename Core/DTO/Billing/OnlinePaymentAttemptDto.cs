namespace Core.DTO.Billing;

public class OnlinePaymentAttemptDto
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public long PatientId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "";
    public string? GatewayTransactionId { get; set; }
    public long? PaymentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
