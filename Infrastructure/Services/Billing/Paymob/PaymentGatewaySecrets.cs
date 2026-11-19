using System.Security.Cryptography;
using Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Billing.Paymob;

/// <summary>Encrypts the clinic's gateway secrets at rest (ASP.NET Data Protection) and loads them for the current tenant.</summary>
public sealed class PaymentGatewaySecrets
{
    private const string Purpose = "Shifa.PaymentGateway.v1";

    private readonly BillingDbContext _context;
    private readonly IDataProtector _protector;

    public PaymentGatewaySecrets(BillingDbContext context, IDataProtectionProvider dataProtection)
    {
        _context = context;
        _protector = dataProtection.CreateProtector(Purpose);
    }

    public string Protect(string secret) => _protector.Protect(secret);

    /// <summary>The current tenant's keys, or null when the clinic hasn't configured a gateway.</summary>
    public async Task<PaymobCredentials?> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.PaymentGatewaySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
            return null;

        try
        {
            return new PaymobCredentials(settings.PublicKey, _protector.Unprotect(settings.SecretKeyProtected),
                _protector.Unprotect(settings.HmacSecretProtected), ParseIds(settings.IntegrationIds));
        }
        catch (CryptographicException ex)
        {
            // The Data Protection key ring changed (not persisted / not shared): the clinic must re-enter its keys.
            throw new InvalidOperationException("The stored payment gateway secrets can't be decrypted.", ex);
        }
    }

    public static string FormatIds(IEnumerable<long> ids) => string.Join(',', ids.Distinct());

    public static IReadOnlyList<long> ParseIds(string ids) =>
        ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(long.Parse).ToList();
}
