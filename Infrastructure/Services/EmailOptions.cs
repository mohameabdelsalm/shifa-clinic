namespace Infrastructure.Services;

/// <summary>The "Email" configuration section. Without SmtpHost only Development can start (fake sender).</summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public string From { get; set; } = "no-reply@shifa.local";
    public string FromName { get; set; } = "Shifa";
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;

    /// <summary>None (Mailpit locally on 1025), StartTls (port 587), SslOnConnect (port 465), or Auto.</summary>
    public string SmtpSecurity { get; set; } = "StartTls";
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }
}
