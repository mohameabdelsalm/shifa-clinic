using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>Requires a patient portal token (api/portal/... endpoints): a patient or guardian who signed in with a phone OTP.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PortalUserAttribute : AuthorizeAttribute
{
    public const string PolicyName = "PortalUser";

    public PortalUserAttribute() : base(PolicyName)
    {
    }
}
