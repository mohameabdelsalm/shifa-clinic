using Core.Entities.Common;
using Core.Enums.Identity;

namespace Core.Entities.Identity;

/// <summary>
/// A one-time link to set a password (reset or invitation), stored only as a SHA-256 hash (ADR-014).
/// Platform-owned like <see cref="User"/>: accounts are global.
/// </summary>
[PlatformOwned]
public class PasswordToken : BaseModel
{
    public static readonly TimeSpan ResetLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    public long UserId { get; set; }
    public PasswordTokenPurposeEnum Purpose { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Set when the token is used, or when it is replaced/invalidated.</summary>
    public DateTimeOffset? UsedAt { get; set; }
}
