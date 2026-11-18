using Core.Interfaces.Common;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Services;

/// <summary>
/// Plain-text e-mail over SMTP with MailKit (ADR-015; Microsoft recommends it over System.Net.Mail.SmtpClient).
/// Works with Mailpit locally and any SMTP provider. Logs the failure type only, never the recipient or body.
/// </summary>
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
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_options.SmtpHost ?? throw new InvalidOperationException("Email:SmtpHost is not configured."), _options.SmtpPort, Enum.Parse<SecureSocketOptions>(_options.SmtpSecurity, ignoreCase: true),
                cancellationToken);
            if (!string.IsNullOrEmpty(_options.SmtpUserName))
                await client.AuthenticateAsync(_options.SmtpUserName, _options.SmtpPassword ?? "", cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("Sending an e-mail failed: {Error}", ex.GetType().Name);
            throw;
        }
    }
}
