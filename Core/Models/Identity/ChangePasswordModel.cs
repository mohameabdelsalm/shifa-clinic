namespace Core.Models.Identity;

public class ChangePasswordModel
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
