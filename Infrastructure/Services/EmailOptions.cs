namespace Infrastructure.Services;

/// <summary>The "Email" configuration section. Without Smtp:Host only Development can start (fake sender).</summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    public string From { get; set; } = "no-reply@shifa.local";
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public bool SmtpEnableSsl { get; set; } = true;
    public string? SmtpUserName { get; set; }
    public string? SmtpPassword { get; set; }
}
