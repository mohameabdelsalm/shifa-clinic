using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Billing;
using Core.Models.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

/// <summary>Online payment attempts from the portal, for staff follow-up (ADR-013).</summary>
[ApiController]
[RequireFeature(FeatureCodes.Billing)]
[Route("api/online-payments")]
public class OnlinePaymentController : ControllerBase
{
    private readonly IOnlinePaymentRepo _repo;

    public OnlinePaymentController(IOnlinePaymentRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.BillingRead)]
    public async Task<Response> Search(SearchOnlinePaymentsModel model, CancellationToken ct) => await _repo.Search(model, ct);

    /// <summary>Returns a NotApplied online payment (paid online after the invoice was settled) to the patient.</summary>
    [HttpPost("{id:long}/refund")]
    [Permission(Permissions.BillingRefund)]
    public async Task<Response> RefundNotApplied(long id, CancellationToken ct) => await _repo.RefundNotApplied(id, ct);
}
