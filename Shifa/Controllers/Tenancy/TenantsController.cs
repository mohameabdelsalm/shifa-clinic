using Core.Common;
using Core.Interfaces.Tenancy;
using Core.Models.Tenancy;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Tenancy;

[ApiController]
[PlatformAdmin]
[Route("api/platform/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ITenantRepo _tenantRepo;

    public TenantsController(ITenantRepo tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    [HttpPost("search")]
    public async Task<Response> Search(SearchTenantsModel model, CancellationToken cancellationToken) => await _tenantRepo.Search(model, cancellationToken);

    [HttpGet("{id}")]
    public async Task<Response> View(long id, CancellationToken cancellationToken) => await _tenantRepo.View(id, cancellationToken);

    [HttpPost]
    public async Task<Response> Add(AddTenantModel model, CancellationToken cancellationToken) => await _tenantRepo.Add(model, cancellationToken);

    [HttpPut("{id}/suspend")]
    public async Task<Response> Suspend(long id, CancellationToken cancellationToken) => await _tenantRepo.Suspend(id, cancellationToken);

    [HttpPut("{id}/activate")]
    public async Task<Response> Activate(long id, CancellationToken cancellationToken) => await _tenantRepo.Activate(id, cancellationToken);

    [HttpPut("plan")]
    public async Task<Response> ChangePlan(ChangePlanModel model, CancellationToken cancellationToken) => await _tenantRepo.ChangePlan(model, cancellationToken);

    [HttpPost("payments")]
    public async Task<Response> RecordPayment(RecordPaymentModel model, CancellationToken cancellationToken) => await _tenantRepo.RecordPayment(model, cancellationToken);
}
