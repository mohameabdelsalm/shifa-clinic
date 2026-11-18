using Core.Entities.Common;
using Core.Enums.Billing;

namespace Core.Entities.Billing;

/// <summary>One "pay online" from the portal: the intention we created and what the gateway callback said (ADR-013).</summary>
public class OnlinePaymentAttempt : TenantOwnedEntity
{
    public long InvoiceId { get; set; }
    public long PatientId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "EGP";

    /// <summary>Our reference sent as special_reference; it comes back as merchant_order_id in the callback.</summary>
    public string Reference { get; set; } = "";
    public OnlineAttemptStatusEnum Status { get; set; } = OnlineAttemptStatusEnum.Pending;
    public string? GatewayIntentionId { get; set; }

    /// <summary>Paymob's order id for the intention: a callback must carry it (merchant_order_id alone isn't signed, ADR-017).</summary>
    public string? GatewayOrderId { get; set; }

    /// <summary>What the gateway actually captured (from the signed amount_cents); a NotApplied refund returns this.</summary>
    public decimal? CapturedAmount { get; set; }
    public string? GatewayTransactionId { get; set; }
    public long? PaymentId { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
