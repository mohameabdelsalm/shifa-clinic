using Shifa.Core.Common;
using Shifa.Identity.Models;

namespace Shifa.Identity.Interfaces;

public interface IAuthRepo
{
    Task<Response> Login(LoginModel model, CancellationToken cancellationToken);
    Task<Response> Refresh(RefreshTokenModel model, CancellationToken cancellationToken);
    Task<Response> SwitchTenant(SwitchTenantModel model, CancellationToken cancellationToken);
    Task<Response> Logout(RefreshTokenModel model, CancellationToken cancellationToken);
}

public interface IRoleRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> GetPermissions();
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddRoleModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateRoleModel model, CancellationToken cancellationToken);
    Task<Response> Delete(long id, CancellationToken cancellationToken);
}

public interface IStaffRepo
{
    Task<Response> Search(SearchStaffModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddStaffModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateStaffModel model, CancellationToken cancellationToken);
    Task<Response> Suspend(long id, CancellationToken cancellationToken);
    Task<Response> Activate(long id, CancellationToken cancellationToken);
}
