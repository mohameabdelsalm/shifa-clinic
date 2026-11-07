namespace Shifa.Core.Common;

/// <summary>Bound from the "Jwt" configuration section. SigningKey is a secret: user-secrets or a secret store, never committed.</summary>
public class JwtSettings
{
    public string Issuer { get; set; } = "Shifa";
    public string Audience { get; set; } = "Shifa.Clients";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
    public string? SigningKey { get; set; }
}
