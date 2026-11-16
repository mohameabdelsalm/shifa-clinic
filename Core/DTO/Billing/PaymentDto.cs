namespace Core.DTO.Billing;

public class PaymentDto
{
    public long Id { get; set; }
    public long InvoiceId { get; set; }
    public string Number { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Method { get; set; } = "";
    public string? Reference { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public long? CashierShiftId { get; set; }
    public long? ReceivedByUserId { get; set; }
}
