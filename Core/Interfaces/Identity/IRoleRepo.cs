using Core.Common;
using Core.Models.Identity;

namespace Core.Interfaces.Identity;

public interface IRoleRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> GetPermissions();
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddRoleModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateRoleModel model, CancellationToken cancellationToken);
    Task<Response> Delete(long id, CancellationToken cancellationToken);
}
