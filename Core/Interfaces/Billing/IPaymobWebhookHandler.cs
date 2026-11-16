namespace Core.Interfaces.Billing;

public enum WebhookOutcome
{
    /// <summary>Signature valid; processed or safely ignored.</summary>
    Accepted,

    /// <summary>Unknown tenant/settings or a wrong HMAC: nothing done.</summary>
    Rejected,
}

/// <summary>Handles Paymob transaction callbacks (ADR-013). Anonymous: trust comes only from the HMAC.</summary>
public interface IPaymobWebhookHandler
{
    Task<WebhookOutcome> HandleAsync(long tenantId, string? hmac, string body, CancellationToken cancellationToken);
}
