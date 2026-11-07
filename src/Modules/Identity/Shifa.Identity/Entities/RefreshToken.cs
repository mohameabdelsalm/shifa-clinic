using Shifa.Core.Entities.Common;

namespace Shifa.Identity.Entities;

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
    public string TokenHash { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public long? ReplacedById { get; set; }
}

/// <summary>Append-only audit trail. TenantId is the tenant of the actor, TargetTenantId the tenant acted upon (Platform Admin actions).</summary>
[PlatformOwned]
public class AuditLog : BaseModel
{
    public long? UserId { get; set; }
    public long? TenantId { get; set; }
    public long? TargetTenantId { get; set; }
    public string Action { get; set; } = "";
    public string? Entity { get; set; }
    public long? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
}
