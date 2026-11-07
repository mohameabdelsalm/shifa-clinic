using Microsoft.AspNetCore.Mvc;
using Shifa.Core.Common;
using Shifa.Infrastructure.Auth;
using Shifa.Patients.Interfaces;
using Shifa.Patients.Models;

namespace Shifa.Patients.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientController : ControllerBase
{
    private readonly IPatientRepo _repo;

    public PatientController(IPatientRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("search")]
    [Permission(Permissions.PatientsRead)]
    public async Task<Response> Search(SearchPatientsModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.PatientsRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPost]
    [Permission(Permissions.PatientsCreate)]
    public async Task<Response> Add(AddPatientModel model, CancellationToken ct) => await _repo.Add(model, ct);

    [HttpPut]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> Update(UpdatePatientModel model, CancellationToken ct) => await _repo.Update(model, ct);

    [HttpPost("guardians")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> AddGuardian(AddGuardianModel model, CancellationToken ct) => await _repo.AddGuardian(model, ct);

    [HttpDelete("guardians/{id}")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> DeleteGuardian(long id, CancellationToken ct) => await _repo.DeleteGuardian(id, ct);

    [HttpPost("consents")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> GrantConsent(GrantConsentModel model, CancellationToken ct) => await _repo.GrantConsent(model, ct);

    [HttpPut("consents/{id}/revoke")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> RevokeConsent(long id, CancellationToken ct) => await _repo.RevokeConsent(id, ct);

    [HttpPost("portal-access")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> LinkPortalAccess(LinkPortalAccessModel model, CancellationToken ct) => await _repo.LinkPortalAccess(model, ct);

    [HttpDelete("portal-access/{id}")]
    [Permission(Permissions.PatientsUpdate)]
    public async Task<Response> RemovePortalAccess(long id, CancellationToken ct) => await _repo.RemovePortalAccess(id, ct);
}

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
