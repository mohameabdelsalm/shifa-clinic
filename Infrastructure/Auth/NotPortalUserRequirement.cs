using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Auth;

/// <summary>Used by the default and fallback policies: any signed-in caller except a patient portal user.</summary>
public sealed class NotPortalUserRequirement : IAuthorizationRequirement
{
}
