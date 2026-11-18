namespace Core.Common;

/// <summary>Bound from the "Jwt" configuration section. SigningKey is a secret: user-secrets or a secret store, never committed.</summary>
public class JwtSettings
{
    public string Issuer { get; set; } = "Shifa";
    public string Audience { get; set; } = "Shifa.Clients";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public string? SigningKey { get; set; }

    /// <summary>
    /// Key rotation (ADR-017): after replacing SigningKey, put the old key here so tokens it signed stay valid until they
    /// expire (at most AccessTokenMinutes); remove it afterwards. New tokens are always signed with SigningKey.
    /// </summary>
    public List<string> PreviousSigningKeys { get; set; } = [];
}
