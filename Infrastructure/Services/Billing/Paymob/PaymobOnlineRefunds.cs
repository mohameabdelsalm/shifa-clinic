using System.Net;
using Core;
using Core.Common;
using Core.Entities.Billing;
using Core.Interfaces.Billing;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Refunds an online payment back to the card/wallet through the clinic's Paymob account (ADR-013). Works even if
/// the OnlinePayments feature was turned off later: money already taken must still be returnable.
/// </summary>
public sealed class PaymobOnlineRefunds : IOnlineRefunds
{
    private readonly PaymentGatewaySecrets _secrets;
    private readonly IPaymobClient _client;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PaymobOnlineRefunds(PaymentGatewaySecrets secrets, IPaymobClient client, IStringLocalizer<LanguageResource> localizer)
    {
        _secrets = secrets;
        _client = client;
        _localizer = localizer;
    }

    public async Task<string> RefundAsync(Payment payment, decimal amount, CancellationToken cancellationToken)
    {
        var credentials = await _secrets.LoadAsync(cancellationToken)
            ?? throw new BusinessException(_localizer["OnlinePaymentsNotConfigured"].Value, HttpStatusCode.BadRequest);

        PaymobRefundResult result;
        try
        {
            result = await _client.RefundAsync(credentials, payment.Reference ?? "", PaymobAmounts.ToCents(amount), cancellationToken);
        }
        catch (PaymobException)
        {
            throw new BusinessException(_localizer["PaymentGatewayError"].Value, HttpStatusCode.BadGateway);
        }

        if (!result.Success)
            throw new BusinessException(_localizer["OnlineRefundFailed"].Value, HttpStatusCode.BadRequest);
        return result.TransactionId ?? "";
    }
}
