using Core.Interfaces.Common;
using Infrastructure.Services;

namespace Shifa.Extensions;

public static class EmailServiceExtensions
{
    /// <summary>
    /// SMTP when Email:SmtpHost is set; otherwise the in-memory fake, which only Development may use (password links
    /// must never silently go nowhere in production).
    /// </summary>
    public static IServiceCollection AddShifaEmail(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.Section));
        services.AddSingleton<InMemoryEmailOutbox>();

        if (!string.IsNullOrWhiteSpace(configuration[$"{EmailOptions.Section}:SmtpHost"]))
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        else if (environment.IsDevelopment())
            services.AddSingleton<IEmailSender, FakeEmailSender>();
        else
            throw new InvalidOperationException("Email:SmtpHost is not configured.");

        return services;
    }
}
