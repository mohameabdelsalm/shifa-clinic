using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Core.Common;
using Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services.Identity;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly JwtKeyProvider _keys;
    private readonly TimeProvider _time;

    public TokenService(IOptions<JwtSettings> settings, JwtKeyProvider keys, TimeProvider time)
    {
        _settings = settings.Value;
        _keys = keys;
        _time = time;
    }

    public (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(
        long userId, string userName, long? tenantId, long? membershipId, bool isPlatformAdmin, string? portalPhoneNumber = null)
    {
        var now = _time.GetUtcNow();
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(ShifaClaimTypes.UserId, userId.ToString()),
            new(ShifaClaimTypes.UserName, userName),
        };
        if (tenantId is not null)
            claims.Add(new Claim(ShifaClaimTypes.TenantId, tenantId.Value.ToString()));
        if (membershipId is not null)
            claims.Add(new Claim(ShifaClaimTypes.MembershipId, membershipId.Value.ToString()));
        if (isPlatformAdmin)
            claims.Add(new Claim(ShifaClaimTypes.PlatformAdmin, "true"));
        if (portalPhoneNumber is not null)
        {
            claims.Add(new Claim(ShifaClaimTypes.PatientPortal, "true"));
            claims.Add(new Claim(ShifaClaimTypes.PhoneNumber, portalPhoneNumber));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(_keys.Key, SecurityAlgorithms.HmacSha256),
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }

    public (string Raw, string Hash) CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (raw, HashRefreshToken(raw));
    }

    public string HashRefreshToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    public DateTimeOffset RefreshTokenExpiry() => _time.GetUtcNow().AddDays(_settings.RefreshTokenDays);

    public string CreateOtpCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string HashOtp(long tenantId, string phoneNumber, string code) =>
        Convert.ToHexString(HMACSHA256.HashData(_keys.Key.Key, Encoding.UTF8.GetBytes($"otp|{tenantId}|{phoneNumber}|{code}")));
}
