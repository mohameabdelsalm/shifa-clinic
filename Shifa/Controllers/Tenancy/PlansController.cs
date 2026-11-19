using Core.Common;
using Core.Interfaces.Tenancy;
using Core.Models.Tenancy;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Tenancy;

[ApiController]
[PlatformAdmin]
[Route("api/platform/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanRepo _planRepo;

    public PlansController(IPlanRepo planRepo)
    {
        _planRepo = planRepo;
    }

    [HttpGet]
    public async Task<Response> GetAll(CancellationToken cancellationToken) => await _planRepo.GetAll(cancellationToken);

    [HttpGet("{id}")]
    public async Task<Response> View(long id, CancellationToken cancellationToken) => await _planRepo.View(id, cancellationToken);

    [HttpPost]
    public async Task<Response> Add(AddPlanModel model, CancellationToken cancellationToken) => await _planRepo.Add(model, cancellationToken);

    [HttpPut]
    public async Task<Response> Update(UpdatePlanModel model, CancellationToken cancellationToken) => await _planRepo.Update(model, cancellationToken);
}
