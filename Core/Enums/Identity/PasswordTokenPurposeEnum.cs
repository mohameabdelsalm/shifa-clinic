namespace Core.Enums.Identity;

public enum PasswordTokenPurposeEnum
{
    /// <summary>"Forgot password": valid 30 minutes.</summary>
    Reset = 1,

    /// <summary>A new staff member sets their first password: valid 72 hours.</summary>
    Invitation,
}
