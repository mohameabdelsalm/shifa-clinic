using Core.Common;
using Core.Models.Identity;

namespace Core.Interfaces.Identity;

public interface IAuthRepo
{
    Task<Response> Login(LoginModel model, CancellationToken cancellationToken);
    Task<Response> Refresh(RefreshTokenModel model, CancellationToken cancellationToken);
    Task<Response> SwitchTenant(SwitchTenantModel model, CancellationToken cancellationToken);
    Task<Response> Logout(RefreshTokenModel model, CancellationToken cancellationToken);
    Task<Response> RequestPortalOtp(RequestOtpModel model, CancellationToken cancellationToken);
    Task<Response> VerifyPortalOtp(VerifyOtpModel model, CancellationToken cancellationToken);
}
