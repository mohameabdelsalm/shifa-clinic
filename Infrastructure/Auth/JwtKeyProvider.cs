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
    public SymmetricSecurityKey Key { get; }

    public JwtKeyProvider(IOptions<JwtSettings> settings, IHostEnvironment environment, ILogger<JwtKeyProvider> logger)
    {
        var configured = settings.Value.SigningKey;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Encoding.UTF8.GetByteCount(configured) < 32)
                throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
            Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configured));
            return;
        }

        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        logger.LogWarning("Jwt:SigningKey is not set; using a temporary key for this process (Development only).");
        Key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(64));
    }
}
