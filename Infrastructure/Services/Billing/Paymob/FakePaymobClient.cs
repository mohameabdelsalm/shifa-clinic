namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Development only (Paymob:UseFake=true): synthetic intentions and refunds so the flow can be tried without merchant
/// keys. Callbacks are simulated by posting a signed body to the webhook. Refused outside Development (Program.cs).
/// A refund of an amount ending in .13 piasters is "pending" and .14 is "refused", so those paths can be exercised.
/// </summary>
public sealed class FakePaymobClient : IPaymobClient
{
    public Task<PaymobIntention> CreateIntentionAsync(PaymobCredentials credentials, PaymobIntentionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymobIntention($"fake-{Guid.NewGuid():N}", $"fake_csk_{Guid.NewGuid():N}", Random.Shared.NextInt64(10_000_000, 99_999_999).ToString()));

    public Task<PaymobRefundResult> RefundAsync(PaymobCredentials credentials, string transactionId, long amountCents, CancellationToken cancellationToken)
    {
        var id = Random.Shared.NextInt64(100_000_000, 999_999_999).ToString();
        return Task.FromResult((amountCents % 100) switch
        {
            13 => new PaymobRefundResult(false, true, id),
            14 => new PaymobRefundResult(false, false, id),
            _ => new PaymobRefundResult(true, false, id),
        });
    }
}
