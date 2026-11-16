using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Billing;
using Core.Models.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

/// <summary>The clinic's own Paymob account (ADR-013). Secrets can be replaced but are never returned.</summary>
[ApiController]
[RequireFeature(FeatureCodes.Billing)]
[RequireFeature(FeatureCodes.OnlinePayments)]
[Permission(Permissions.BillingSettings)]
[Route("api/payment-gateway")]
public class PaymentGatewayController : ControllerBase
{
    private readonly IPaymentGatewayRepo _repo;

    public PaymentGatewayController(IPaymentGatewayRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> Get(CancellationToken ct) => await _repo.Get(ct);

    [HttpPut]
    public async Task<Response> Save(SavePaymentGatewayModel model, CancellationToken ct) => await _repo.Save(model, ct);
}
