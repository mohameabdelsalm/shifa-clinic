using Core.Entities.Common;

namespace Core.Entities.Identity;

/// <summary>
/// A refresh token, stored only as a SHA-256 hash. Tokens rotate on every use and belong to a family;
/// presenting an already-used token revokes the whole family (theft detection).
/// Platform-owned because a user's tokens exist outside any tenant context; TenantId says which tenant the token is for.
/// </summary>
[PlatformOwned]
public class RefreshToken : BaseModel
{
    public long UserId { get; set; }

    /// <summary>Null for Platform Admin tokens.</summary>
    public long? TenantId { get; set; }
    public long? MembershipId { get; set; }
    public Guid FamilyId { get; set; }

    /// <summary>True for patient portal sessions (phone OTP): refreshed against the tenant and user, not a membership.</summary>
    public bool IsPortal { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public long? ReplacedById { get; set; }
}
