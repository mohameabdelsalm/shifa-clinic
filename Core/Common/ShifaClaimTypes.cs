namespace Core.Common;

/// <summary>Claim names in Shifa access tokens. All are set by the server at login, never by the client.</summary>
public static class ShifaClaimTypes
{
    public const string UserId = "sub";
    public const string UserName = "name";

    /// <summary>The tenant the token was issued for.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The membership (user + tenant) the token was issued for. Staff tokens only.</summary>
    public const string MembershipId = "membership_id";

    /// <summary>"true" on Platform Admin tokens (no tenant). Re-validated on every request.</summary>
    public const string PlatformAdmin = "platform_admin";

    /// <summary>"true" on patient portal tokens: a tenant, a verified phone number, no membership and no staff permissions.</summary>
    public const string PatientPortal = "patient_portal";

    /// <summary>The verified, normalized phone number of a portal token.</summary>
    public const string PhoneNumber = "phone_number";
}
