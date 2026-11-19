namespace Core.Models.Identity;

/// <summary>Sets a password with the token from a reset or invitation e-mail.</summary>
public class ResetPasswordModel
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
