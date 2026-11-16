using Core.Entities.Billing;

namespace Core.Interfaces.Billing;

/// <summary>Sends a refund of an online payment back through the payment gateway (Paymob).</summary>
public interface IOnlineRefunds
{
    /// <summary>Refunds <paramref name="amount"/> of the gateway transaction in <see cref="Payment.Reference"/>; returns the gateway's refund id.</summary>
    Task<string> RefundAsync(Payment payment, decimal amount, CancellationToken cancellationToken);
}
