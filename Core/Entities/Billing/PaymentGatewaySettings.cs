using Core.Entities.Common;

namespace Core.Entities.Billing;

/// <summary>
/// A clinic's own Paymob merchant settings (ADR-013). Secret key and HMAC secret are stored encrypted (ASP.NET Data
/// Protection) and are never returned by the API.
/// </summary>
public class PaymentGatewaySettings : TenantOwnedEntity
{
    public string Provider { get; set; } = "Paymob";
    public string PublicKey { get; set; } = "";
    public string SecretKeyProtected { get; set; } = "";
    public string HmacSecretProtected { get; set; } = "";

    /// <summary>Comma-separated Paymob integration ids (card, wallet…).</summary>
    public string IntegrationIds { get; set; } = "";
    public long UpdatedByUserId { get; set; }
}
