using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Billing;
using Core.Models.Billing;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Billing;

[ApiController]
[RequireFeature(FeatureCodes.Billing)]
[Route("api/cashier-shifts")]
public class CashierShiftController : ControllerBase
{
    private readonly ICashierShiftRepo _repo;

    public CashierShiftController(ICashierShiftRepo repo)
    {
        _repo = repo;
    }

    [HttpPost]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> Open(OpenShiftModel model, CancellationToken ct) => await _repo.Open(model, ct);

    /// <summary>The caller's open shift at the branch (data is null when none is open).</summary>
    [HttpGet("current/{branchId}")]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> Current(long branchId, CancellationToken ct) => await _repo.Current(branchId, ct);

    [HttpPut("close")]
    [Permission(Permissions.BillingCollect)]
    public async Task<Response> Close(CloseShiftModel model, CancellationToken ct) => await _repo.Close(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.BillingRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPost("search")]
    [Permission(Permissions.BillingRead)]
    public async Task<Response> Search(SearchShiftsModel model, CancellationToken ct) => await _repo.Search(model, ct);
}
