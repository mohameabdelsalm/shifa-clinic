using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Billing;
using Core.Models.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

[ApiController]
[RequireFeature(FeatureCodes.Billing)]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    private readonly IPaymentRepo _repo;

    public PaymentController(IPaymentRepo repo)
    {
        _repo = repo;
    }

    /// <summary>Records a cash, card or wallet payment in the caller's open shift. Needs an Idempotency-Key header.</summary>
    [HttpPost]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> Pay(AddPaymentModel model, [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey, CancellationToken ct) =>
        await _repo.Pay(model, idempotencyKey, ct);

    /// <summary>Refunds (part of) a payment. Needs an Idempotency-Key header.</summary>
    [HttpPost("refunds")]
    [Permission(Permissions.BillingRefund)]
    public async Task<Response> Refund(AddRefundModel model, [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey, CancellationToken ct) =>
        await _repo.Refund(model, idempotencyKey, ct);
}
