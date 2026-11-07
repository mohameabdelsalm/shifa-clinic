using Core.Common;
using Core.Models.Identity;

namespace Core.Interfaces.Identity;

public interface IStaffRepo
{
    Task<Response> Search(SearchStaffModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddStaffModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateStaffModel model, CancellationToken cancellationToken);
    Task<Response> Suspend(long id, CancellationToken cancellationToken);
    Task<Response> Activate(long id, CancellationToken cancellationToken);
}
