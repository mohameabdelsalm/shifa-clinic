namespace Infrastructure.Services.Billing.Paymob;

/// <summary>A clinic's decrypted Paymob keys. Lives only in memory for one request; never logged.</summary>
public sealed record PaymobCredentials(string PublicKey, string SecretKey, string HmacSecret, IReadOnlyList<long> IntegrationIds);
