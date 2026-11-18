using Core.DTO.Billing;
using Core.Entities.Billing;
using Core.Enums.Billing;
using Core.Interfaces.Billing;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Refunds an online payment through the clinic's Paymob account (ADR-013, ADR-017). Works even if the OnlinePayments
/// feature was turned off later: money already taken must still be returnable. A missing answer is Unknown, never
/// "not refunded", so a retry can't send the money back twice.
/// </summary>
public sealed class PaymobOnlineRefunds : IOnlineRefunds
{
    private readonly PaymentGatewaySecrets _secrets;
    private readonly IPaymobClient _client;
    private readonly ILogger<PaymobOnlineRefunds> _logger;

    public PaymobOnlineRefunds(PaymentGatewaySecrets secrets, IPaymobClient client, ILogger<PaymobOnlineRefunds> logger)
    {
        _secrets = secrets;
        _client = client;
        _logger = logger;
    }

    public async Task<OnlineRefundResult> RefundAsync(Payment payment, decimal amount, CancellationToken cancellationToken)
    {
        var credentials = await _secrets.LoadAsync(cancellationToken);
        if (credentials is null)
            return new OnlineRefundResult(OnlineRefundOutcomeEnum.Refused, null);

        try
        {
            var result = await _client.RefundAsync(credentials, payment.Reference ?? "", PaymobAmounts.ToCents(amount), cancellationToken);
            if (result.Success)
                return new OnlineRefundResult(OnlineRefundOutcomeEnum.Succeeded, result.TransactionId);
            return new OnlineRefundResult(result.Pending ? OnlineRefundOutcomeEnum.Unknown : OnlineRefundOutcomeEnum.Refused, result.TransactionId);
        }
        catch (PaymobException)
        {
            // No (usable) answer: Paymob may or may not have refunded.
            _logger.LogWarning("Paymob refund for payment {PaymentId} has no definite answer", payment.Id);
            return new OnlineRefundResult(OnlineRefundOutcomeEnum.Unknown, null);
        }
    }
}
