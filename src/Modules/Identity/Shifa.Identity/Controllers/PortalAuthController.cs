using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shifa.Core.Common;
using Shifa.Identity.Interfaces;
using Shifa.Identity.Models;

namespace Shifa.Identity.Controllers;

/// <summary>
/// Patient and guardian sign-in with a phone OTP. Refreshing and signing out use the shared api/auth endpoints.
/// Both endpoints are public and rate limited per IP.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/portal/auth")]
[EnableRateLimiting("auth")]
public class PortalAuthController : ControllerBase
{
    private readonly IAuthRepo _authRepo;

    public PortalAuthController(IAuthRepo authRepo)
    {
        _authRepo = authRepo;
    }

    [HttpPost("otp/request")]
    public async Task<Response> RequestOtp(RequestOtpModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.RequestPortalOtp(model, cancellationToken);
    }

    [HttpPost("otp/verify")]
    public async Task<Response> VerifyOtp(VerifyOtpModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.VerifyPortalOtp(model, cancellationToken);
    }
}
