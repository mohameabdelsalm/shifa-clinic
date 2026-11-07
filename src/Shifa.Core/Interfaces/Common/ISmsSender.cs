namespace Shifa.Core.Interfaces.Common;

/// <summary>
/// Sends a text message. Local development uses a fake that keeps messages in memory (and exposes them
/// on a Development-only endpoint); a real provider replaces it in production through configuration.
/// Never log the message text: it can contain a one-time code.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
