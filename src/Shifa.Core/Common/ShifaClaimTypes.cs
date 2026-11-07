namespace Shifa.Core.Common;

/// <summary>Claim names in Shifa access tokens. All are set by the server at login, never by the client.</summary>
public static class ShifaClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "name";

    /// <summary>The tenant the token was issued for.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The membership (user + tenant) the token was issued for.</summary>
    public const string MembershipId = "membership_id";

    /// <summary>"true" on Platform Admin tokens (no tenant). Re-validated on every request.</summary>
    public const string PlatformAdmin = "platform_admin";
}
