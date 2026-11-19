using Core.Common;
using Core.Models.Billing;

namespace Core.Interfaces.Billing;

public interface ICashierShiftRepo
{
    Task<Response> Open(OpenShiftModel model, CancellationToken cancellationToken);
    Task<Response> Current(long branchId, CancellationToken cancellationToken);
    Task<Response> Close(CloseShiftModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Search(SearchShiftsModel model, CancellationToken cancellationToken);
}
