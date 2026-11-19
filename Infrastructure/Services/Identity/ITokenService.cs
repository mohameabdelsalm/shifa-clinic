namespace Infrastructure.Services.Identity;

public interface ITokenService
{
    /// <summary>
    /// A signed access token. Staff: tenant + membership. Platform Admin: the platform flag only.
    /// Portal (patient/guardian): tenant + verified phone number, no membership.
    /// </summary>
    (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(long userId, string userName, long? tenantId, long? membershipId,
        bool isPlatformAdmin, string? portalPhoneNumber = null);

    /// <summary>A random refresh token for the client and its SHA-256 hash for the database.</summary>
    (string Raw, string Hash) CreateRefreshToken();

    string HashRefreshToken(string raw);
    DateTimeOffset RefreshTokenExpiry();

    /// <summary>A short numeric code and its keyed hash (HMAC-SHA256 with the signing key, bound to tenant and phone).</summary>
    string CreateOtpCode();
    string HashOtp(long tenantId, string phoneNumber, string code);
}
