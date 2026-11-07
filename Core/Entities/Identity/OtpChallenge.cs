using Core.Entities.Common;

namespace Core.Entities.Identity;

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
