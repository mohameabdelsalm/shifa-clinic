using Shifa.Core.Common;
using Shifa.Tenancy.Models;

namespace Shifa.Tenancy.Interfaces;

public interface IPlanRepo
{
    Task<Response> GetAll(CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddPlanModel model, CancellationToken cancellationToken);
    Task<Response> Update(UpdatePlanModel model, CancellationToken cancellationToken);
}

public interface ITenantRepo
{
    Task<Response> Search(SearchTenantsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddTenantModel model, CancellationToken cancellationToken);
    Task<Response> Suspend(long id, CancellationToken cancellationToken);
    Task<Response> Activate(long id, CancellationToken cancellationToken);
    Task<Response> ChangePlan(ChangePlanModel model, CancellationToken cancellationToken);
    Task<Response> RecordPayment(RecordPaymentModel model, CancellationToken cancellationToken);
}
