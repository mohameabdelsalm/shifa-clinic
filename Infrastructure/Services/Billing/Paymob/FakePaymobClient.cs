namespace Infrastructure.Services.Billing.Paymob;

/// <summary>
/// Development only (Paymob:UseFake=true): synthetic intentions and refunds so the flow can be tried without merchant
/// keys. Callbacks are simulated by posting a signed body to the webhook. Refused outside Development (Program.cs).
/// </summary>
public sealed class FakePaymobClient : IPaymobClient
{
    public Task<PaymobIntention> CreateIntentionAsync(PaymobCredentials credentials, PaymobIntentionRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymobIntention($"fake-{Guid.NewGuid():N}", $"fake_csk_{Guid.NewGuid():N}"));

    public Task<PaymobRefundResult> RefundAsync(PaymobCredentials credentials, string transactionId, long amountCents, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymobRefundResult(true, Random.Shared.NextInt64(100_000_000, 999_999_999).ToString()));
}
