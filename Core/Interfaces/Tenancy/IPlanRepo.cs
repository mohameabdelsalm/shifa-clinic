using Core.Common;
using Core.Models.Tenancy;

namespace Core.Interfaces.Tenancy;

public interface IPlanRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddPlanModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdatePlanModel model, CancellationToken cancellationToken);
}
