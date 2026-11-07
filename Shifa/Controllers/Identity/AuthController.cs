using Core.Common;
using Core.Interfaces.Identity;
using Core.Models.Identity;
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
}
