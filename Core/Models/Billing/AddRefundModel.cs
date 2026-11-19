namespace Core.Models.Billing;

public class AddRefundModel
{
    public long PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = "";
}
