using Core.Interfaces.Common;
using Infrastructure.Services;

namespace Shifa.Extensions;

public static class SmsServiceExtensions
{
    /// <summary>
    /// Sms:Provider (ADR-017): "Fake" (Development only, the default there) or "Disabled" (no provider chosen yet).
    /// Any other environment must say which one explicitly, so a fake never runs in production unnoticed.
    /// </summary>
    public static IServiceCollection AddShifaSms(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<InMemorySmsOutbox>();
        var provider = configuration["Sms:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
            provider = environment.IsDevelopment() ? "Fake" : throw new InvalidOperationException("Sms:Provider is not configured (Disabled, or a provider).");

        switch (provider.Trim().ToLowerInvariant())
        {
            case "fake" when environment.IsDevelopment():
                services.AddSingleton<ISmsSender, FakeSmsSender>();
                break;
            case "fake":
                throw new InvalidOperationException("Sms:Provider=Fake is only allowed in the Development environment.");
            case "disabled":
                services.AddSingleton<ISmsSender, DisabledSmsSender>();
                break;
            default:
                throw new InvalidOperationException($"Unknown Sms:Provider '{provider}'.");
        }

        return services;
    }
}
