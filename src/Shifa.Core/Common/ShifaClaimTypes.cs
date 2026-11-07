namespace Shifa.Core.Common;

/// <summary>Custom claim names in Shifa access tokens.</summary>
public static class ShifaClaimTypes
{
    /// <summary>The tenant the token was issued for. Set by the server at login, never by the client.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The membership (user + tenant) the token was issued for.</summary>
    public const string MembershipId = "membership_id";
}
