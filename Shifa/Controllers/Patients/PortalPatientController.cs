using Core.Common;
using Core.Interfaces.Patients;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Patients;

/// <summary>Endpoints for patients and guardians signed in with a phone OTP.</summary>
[ApiController]
[PortalUser]
[Route("api/portal/patients")]
public class PortalPatientController : ControllerBase
{
    private readonly IPortalPatientRepo _repo;

    public PortalPatientController(IPortalPatientRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);

    [HttpGet("{id}")]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);
}
