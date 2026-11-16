using Infrastructure.Services.Billing.Paymob;
using Microsoft.AspNetCore.DataProtection;

namespace Shifa.Extensions;

public static class PaymobServiceExtensions
{
    /// <summary>
    /// Paymob client (real, or the fake in Development with Paymob:UseFake=true) and Data Protection for the stored
    /// gateway secrets. Set DataProtection:KeysPath to persist the key ring (required in production, ADR-013).
    /// </summary>
    public static IServiceCollection AddPaymob(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var dataProtection = services.AddDataProtection().SetApplicationName("Shifa");
        if (configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        services.Configure<PaymobOptions>(configuration.GetSection(PaymobOptions.Section));
        services.AddScoped<PaymentGatewaySecrets>();

        var options = configuration.GetSection(PaymobOptions.Section).Get<PaymobOptions>() ?? new PaymobOptions();
        if (options.UseFake)
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("Paymob:UseFake is only allowed in the Development environment.");
            services.AddSingleton<IPaymobClient, FakePaymobClient>();
        }
        else
        {
            services.AddHttpClient<IPaymobClient, PaymobClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            });
        }

        return services;
    }
}
