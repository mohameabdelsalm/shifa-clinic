namespace Shifa.Identity.Models;

public class LoginModel
{
    /// <summary>Email or phone number.</summary>
    public string Identifier { get; set; } = "";
    public string Password { get; set; } = "";

    /// <summary>
    /// The organization to sign in to (its short name). Required for staff; Platform Admins omit it.
    /// This only selects which of the user's memberships to use: the server validates the membership.
    /// </summary>
    public string? TenantSlug { get; set; }
}

public class RequestOtpModel
{
    /// <summary>The clinic (organization short name) the patient wants to sign in to.</summary>
    public string TenantSlug { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
}

public class VerifyOtpModel
{
    public string TenantSlug { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Code { get; set; } = "";
}

public class RefreshTokenModel
{
    public string RefreshToken { get; set; } = "";
}

public class SwitchTenantModel
{
    public string TenantSlug { get; set; } = "";
}
