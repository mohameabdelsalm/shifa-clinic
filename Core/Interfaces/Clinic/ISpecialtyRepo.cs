using Core.Common;
using Core.Enums.Common;
using Core.Models.Clinic;

namespace Core.Interfaces.Clinic;

public interface ISpecialtyRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> Add(AddSpecialtyModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateSpecialtyModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
}
