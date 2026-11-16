namespace Infrastructure.Services.Billing.Paymob;

public sealed record PaymobIntentionRequest(
    long AmountCents,
    string Currency,
    string Reference,
    string ItemName,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    string NotificationUrl,
    string? RedirectionUrl,
    int ExpirySeconds);
