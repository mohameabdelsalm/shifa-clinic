namespace Infrastructure.Services.Billing.Paymob;

public interface IPaymobClient
{
    /// <summary>POST v1/intention/ — throws <see cref="PaymobException"/> on failure.</summary>
    Task<PaymobIntention> CreateIntentionAsync(PaymobCredentials credentials, PaymobIntentionRequest request, CancellationToken cancellationToken);

    /// <summary>POST api/acceptance/void_refund/refund — throws <see cref="PaymobException"/> when Paymob can't be reached.</summary>
    Task<PaymobRefundResult> RefundAsync(PaymobCredentials credentials, string transactionId, long amountCents, CancellationToken cancellationToken);
}
