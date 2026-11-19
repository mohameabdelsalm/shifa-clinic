namespace Infrastructure.Services.Billing.Paymob;

/// <param name="Pending">Paymob accepted it but hasn't finished (e.g. wallets): the outcome is not known yet.</param>
public sealed record PaymobRefundResult(bool Success, bool Pending, string? TransactionId);
