namespace Core.Models.Identity;

public class RequestOtpModel
{
    /// <summary>The clinic (organization short name) the patient wants to sign in to.</summary>
    public string TenantSlug { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
}
