using Core.Common;
using Core.Enums.Common;
using Core.Models.Clinic;

namespace Core.Interfaces.Clinic;

public interface IServiceRepo
{
    Task<Response> Search(SearchServicesModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddServiceModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdateServiceModel model, CancellationToken cancellationToken);
    Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken);
    Task<Response> SetPrice(SetServicePriceModel model, CancellationToken cancellationToken);
}
