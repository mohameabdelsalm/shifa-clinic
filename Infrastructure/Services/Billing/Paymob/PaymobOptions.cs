namespace Infrastructure.Services.Billing.Paymob;

/// <summary>The "Paymob" configuration section (ADR-013). Merchant keys are per clinic, in the database, not here.</summary>
public sealed class PaymobOptions
{
    public const string Section = "Paymob";

    public string BaseUrl { get; set; } = "https://accept.paymob.com/";
    public string CheckoutUrl { get; set; } = "https://eg.checkout.paymob.com/";

    /// <summary>Public base URL of this API (Paymob posts callbacks to {NotificationBaseUrl}/api/webhooks/paymob/{tenantId}).</summary>
    public string? NotificationBaseUrl { get; set; }

    /// <summary>The portal page Paymob returns the patient to after paying (optional).</summary>
    public string? RedirectionUrl { get; set; }

    public int IntentionExpirySeconds { get; set; } = 3600;

    /// <summary>Development only: use <see cref="FakePaymobClient"/> instead of calling Paymob.</summary>
    public bool UseFake { get; set; }
}
