using Core.Common;
using Core.Models.Billing;

namespace Core.Interfaces.Billing;

public interface IPaymentGatewayRepo
{
    Task<Response> Get(CancellationToken cancellationToken);
    Task<Response> Save(SavePaymentGatewayModel model, CancellationToken cancellationToken);
}
