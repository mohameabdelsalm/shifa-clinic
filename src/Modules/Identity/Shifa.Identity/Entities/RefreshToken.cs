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

    /// <summary>True for patient portal sessions (phone OTP): refreshed against the tenant and user, not a membership.</summary>
    public bool IsPortal { get; set; }
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

/// <summary>
/// A one-time code sent by SMS to sign a patient or guardian in to a tenant's portal.
/// Stored only as a keyed hash, valid for 5 minutes, at most 5 wrong tries, used once.
/// Platform-owned because it exists before anyone is signed in; TenantId says which clinic it is for.
/// </summary>
[PlatformOwned]
public class OtpChallenge : BaseModel
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    public long TenantId { get; set; }
    public string PhoneNumber { get; set; } = "";
    public string CodeHash { get; set; } = "";
    public int Attempts { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
