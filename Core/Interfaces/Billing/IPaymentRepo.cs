using Core.Common;
using Core.Models.Billing;

namespace Core.Interfaces.Billing;

public interface IPaymentRepo
{
    Task<Response> Pay(AddPaymentModel model, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Response> Refund(AddRefundModel model, string? idempotencyKey, CancellationToken cancellationToken);
}
