using Core.Common;
using Core.Interfaces.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Tenancy;

/// <summary>The clinic's own subscription, so the apps can warn about the end of the trial and read-only mode (A6).</summary>
[ApiController]
[Authorize]
[Route("api/subscription")]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionRepo _repo;

    public SubscriptionController(ISubscriptionRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken cancellationToken) => await _repo.GetMine(cancellationToken);
}
