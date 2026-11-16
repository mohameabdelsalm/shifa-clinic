using Core.Common;
using Core.Models.Tenancy;

namespace Core.Interfaces.Tenancy;

public interface ITenantRepo
{
    Task<Response> Search(SearchTenantsModel model, CancellationToken cancellationToken);
    Task<Response> View(long id, CancellationToken cancellationToken);
    Task<Response> Add(AddTenantModel model, CancellationToken cancellationToken);
    Task<Response> Suspend(long id, CancellationToken cancellationToken);
    Task<Response> Activate(long id, CancellationToken cancellationToken);
    Task<Response> ChangePlan(ChangePlanModel model, CancellationToken cancellationToken);
    Task<Response> RecordPayment(RecordPaymentModel model, CancellationToken cancellationToken);
    Task<Response> GetFeatures(long tenantId, CancellationToken cancellationToken);
    Task<Response> SetFeature(SetTenantFeatureModel model, CancellationToken cancellationToken);
}
