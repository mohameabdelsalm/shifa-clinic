using Core.DTO.Billing;
using Core.Entities.Billing;

namespace Core.Interfaces.Billing;

/// <summary>Sends a refund of an online payment back through the payment gateway (Paymob).</summary>
public interface IOnlineRefunds
{
    /// <summary>
    /// Refunds <paramref name="amount"/> of the gateway transaction in <see cref="Payment.Reference"/>. Never throws for a
    /// gateway problem: an unclear answer is <see cref="Core.Enums.Billing.OnlineRefundOutcomeEnum.Unknown"/>, so callers
    /// never treat "maybe refunded" as "not refunded".
    /// </summary>
    Task<OnlineRefundResult> RefundAsync(Payment payment, decimal amount, CancellationToken cancellationToken);
}
