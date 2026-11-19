namespace Core.Models.Identity;

public class VerifyOtpModel
{
    public string TenantSlug { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string Code { get; set; } = "";
}
