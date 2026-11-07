using Shifa.Core.Entities.Common;
using Shifa.Core.Enums.Common;

namespace Shifa.Identity.Entities;

/// <summary>
/// A person who can sign in. Global (platform-owned): one account per person, with access to
/// tenants through <see cref="Membership"/> rows (ADR-007). Passwords are hashed with ASP.NET Core's PasswordHasher.
/// </summary>
[PlatformOwned]
public class User : BaseModel
{
    public const int MaxFailedAccessCount = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public string FullName { get; set; } = "";

    /// <summary>Stored lower case.</summary>
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string PasswordHash { get; set; } = "";
    public bool IsPlatformAdmin { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Active;
    public int FailedAccessCount { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd is not null && LockoutEnd > now;

    public void RegisterFailedSignIn(DateTimeOffset now)
    {
        FailedAccessCount++;
        if (FailedAccessCount >= MaxFailedAccessCount)
        {
            LockoutEnd = now + LockoutDuration;
            FailedAccessCount = 0;
        }
    }

    public void RegisterSuccessfulSignIn()
    {
        FailedAccessCount = 0;
        LockoutEnd = null;
    }
}
