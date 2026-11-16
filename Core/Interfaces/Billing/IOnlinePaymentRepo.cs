using Core.Common;
using Core.Models.Billing;

namespace Core.Interfaces.Billing;

public interface IOnlinePaymentRepo
{
    /// <summary>Portal: starts paying the invoice balance online and returns the checkout URL.</summary>
    Task<Response> StartPortalPayment(long invoiceId, CancellationToken cancellationToken);

    /// <summary>Staff: online payment attempts (HRM-style search).</summary>
    Task<Response> Search(SearchOnlinePaymentsModel model, CancellationToken cancellationToken);

    /// <summary>Staff: returns a NotApplied online payment to the patient through the gateway.</summary>
    Task<Response> RefundNotApplied(long attemptId, CancellationToken cancellationToken);
}
