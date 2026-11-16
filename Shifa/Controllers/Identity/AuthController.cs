using Core.Common;
using Core.Interfaces.Identity;
using Core.Models.Identity;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Shifa.Controllers.Identity;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthRepo _authRepo;

    public AuthController(IAuthRepo authRepo)
    {
        _authRepo = authRepo;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<Response> Login(LoginModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.Login(model, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<Response> Refresh(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.Refresh(model, cancellationToken);
    }

    [Authorize]
    [ReadOperation]
    [HttpPost("switch-tenant")]
    public async Task<Response> SwitchTenant(SwitchTenantModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.SwitchTenant(model, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<Response> Logout(RefreshTokenModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.Logout(model, cancellationToken);
    }

    /// <summary>Always answers the same; if the e-mail belongs to an active account, a 30-minute reset link is e-mailed.</summary>
    [AllowAnonymous]
    [HttpPost("password/forgot")]
    public async Task<Response> ForgotPassword(ForgotPasswordModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.ForgotPassword(model, cancellationToken);
    }

    /// <summary>Sets a new password with the token from a reset or invitation e-mail; all sessions are signed out.</summary>
    [AllowAnonymous]
    [HttpPost("password/reset")]
    public async Task<Response> ResetPassword(ResetPasswordModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.ResetPassword(model, cancellationToken);
    }

    /// <summary>Changes the signed-in staff member's password; all sessions are signed out (sign in again).</summary>
    [Authorize]
    [ReadOperation]
    [HttpPut("password")]
    public async Task<Response> ChangePassword(ChangePasswordModel model, CancellationToken cancellationToken)
    {
        return await _authRepo.ChangePassword(model, cancellationToken);
    }
}
