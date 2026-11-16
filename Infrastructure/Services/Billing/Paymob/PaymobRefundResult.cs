namespace Infrastructure.Services.Billing.Paymob;

public sealed record PaymobRefundResult(bool Success, string? TransactionId);
