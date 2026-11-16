namespace Core.DTO.Billing;

/// <summary>Where to send the patient to pay (Paymob Unified Checkout).</summary>
public class OnlineCheckoutDto
{
    public long AttemptId { get; set; }
    public string CheckoutUrl { get; set; } = "";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "";
}
