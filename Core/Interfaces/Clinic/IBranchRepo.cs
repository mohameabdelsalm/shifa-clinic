using Core.Common;
using Core.Enums.Common;
using Core.Models.Clinic;

namespace Core.Interfaces.Clinic;

public interface IBranchRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> Search(SearchBranchesModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddBranchModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateBranchModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
}
