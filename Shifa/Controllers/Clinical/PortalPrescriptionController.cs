using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Clinical;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinical;

/// <summary>Prescriptions for patients and guardians signed in with a phone OTP.</summary>
[ApiController]
[PortalUser]
[RequireFeature(FeatureCodes.Clinical)]
[RequireFeature(FeatureCodes.PatientPortal)]
[Route("api/portal/prescriptions")]
public class PortalPrescriptionController : ControllerBase
{
    private readonly IPortalPrescriptionRepo _repo;

    public PortalPrescriptionController(IPortalPrescriptionRepo repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);
}
