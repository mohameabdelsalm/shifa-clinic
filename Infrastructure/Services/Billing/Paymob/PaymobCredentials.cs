namespace Infrastructure.Services.Billing.Paymob;

/// <summary>A clinic's decrypted Paymob keys. Lives only in memory for one request; never logged.</summary>
public sealed record PaymobCredentials(string PublicKey, string SecretKey, string HmacSecret, IReadOnlyList<long> IntegrationIds)
{
    /// <summary>A record would print every property; never let an accidental log or interpolation show the secrets.</summary>
    public override string ToString() => $"PaymobCredentials {{ PublicKey = {PublicKey}, SecretKey = ***, HmacSecret = *** }}";
}
