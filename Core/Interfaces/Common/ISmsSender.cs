namespace Core.Interfaces.Common;

/// <summary>
/// Sends a text message. Development uses a fake that keeps messages in memory (GET /dev/sms); other environments choose
/// with Sms:Provider (ADR-017): a real provider, or "Disabled" until one is chosen. Never log the message text: it can
/// contain a one-time code.
/// </summary>
public interface ISmsSender
{
    /// <summary>False when SMS is switched off (Sms:Provider=Disabled): callers skip sending instead of failing.</summary>
    bool IsEnabled => true;

    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
