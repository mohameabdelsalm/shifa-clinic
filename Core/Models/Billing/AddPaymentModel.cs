using System.Text.Json.Serialization;
using Core.Enums.Billing;

namespace Core.Models.Billing;

public class AddPaymentModel
{
    public long InvoiceId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Cash, Card or Wallet (online payments come from the gateway, not this endpoint).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentMethodEnum Method { get; set; }

    /// <summary>POS or wallet reference; required for Card and Wallet.</summary>
    public string? Reference { get; set; }
}
