using System.Security.Cryptography;
using System.Text;
using Core.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth;

/// <summary>
/// The token signing key. Production must configure Jwt:SigningKey (a secret, 32+ characters).
/// In Development a random key is generated per process when none is set, so tokens stop working after a restart.
/// </summary>
public sealed class JwtKeyProvider
{
    /// <summary>Signs new tokens.</summary>
    public SymmetricSecurityKey Key { get; }

    /// <summary>Validates tokens: the current key first, then previous keys during a rotation.</summary>
    public IReadOnlyList<SecurityKey> ValidationKeys { get; }

    public JwtKeyProvider(IOptions<JwtSettings> settings, IHostEnvironment environment, ILogger<JwtKeyProvider> logger)
    {
        var configured = settings.Value.SigningKey;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            Key = ToKey(configured, "Jwt:SigningKey");
            ValidationKeys = [Key, .. settings.Value.PreviousSigningKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => ToKey(k, "Jwt:PreviousSigningKeys"))];
            return;
        }

        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        logger.LogWarning("Jwt:SigningKey is not set; using a temporary key for this process (Development only).");
        Key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
        ValidationKeys = [Key];
    }

    private static SymmetricSecurityKey ToKey(string value, string setting) =>
        Encoding.UTF8.GetByteCount(value) < 32
            ? throw new InvalidOperationException($"{setting} must be at least 32 bytes.")
            : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value));
}
