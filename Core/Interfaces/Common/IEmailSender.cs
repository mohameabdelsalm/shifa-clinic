namespace Core.Interfaces.Common;

/// <summary>
/// Sends an e-mail. Development uses a fake that keeps messages in memory (GET /dev/email); other environments
/// need SMTP settings (Email:Smtp). Never log the body: it can contain a password link.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
