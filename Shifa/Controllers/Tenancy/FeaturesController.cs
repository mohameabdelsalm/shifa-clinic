using Core.Common;
using Core.Interfaces.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Tenancy;

/// <summary>The features enabled for the signed-in staff member's clinic (no permission needed; used to build the menu).</summary>
[ApiController]
[Authorize]
[Route("api/features")]
public class FeaturesController : ControllerBase
{
    private readonly IFeatureRepo _repo;

    public FeaturesController(IFeatureRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);
}
