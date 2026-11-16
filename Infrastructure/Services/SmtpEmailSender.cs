using System.Net;
using System.Net.Mail;
using Core.Interfaces.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>Plain-text e-mail over SMTP (Email:SmtpHost…; Mailpit locally on port 1025 without SSL). Logs the failure type only.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(_options.From, to, subject, body);
        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort) { EnableSsl = _options.SmtpEnableSsl };
        if (!string.IsNullOrEmpty(_options.SmtpUserName))
            client.Credentials = new NetworkCredential(_options.SmtpUserName, _options.SmtpPassword);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            _logger.LogError("Sending an e-mail failed: {Error}", ex.GetType().Name);
            throw;
        }
    }
}
