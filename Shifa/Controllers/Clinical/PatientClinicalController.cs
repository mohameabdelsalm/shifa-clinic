using Core.Common;
using Core.Interfaces.Clinical;
using Core.Models.Clinical;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinical;

/// <summary>Patient-level clinical data: allergies, chronic conditions and the clinical summary.</summary>
[ApiController]
[Route("api/patient-clinical")]
public class PatientClinicalController : ControllerBase
{
    private readonly IPatientClinicalRepo _repo;

    public PatientClinicalController(IPatientClinicalRepo repo)
    {
        _repo = repo;
    }

    [HttpGet("{patientId}")]
    [Permission(Permissions.ClinicalRead)]
    public async Task<Response> Summary(long patientId, CancellationToken ct) => await _repo.Summary(patientId, ct);

    [HttpPost("allergies")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> AddAllergy(AddAllergyModel model, CancellationToken ct) => await _repo.AddAllergy(model, ct);

    [HttpDelete("allergies/{id}")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> DeleteAllergy(long id, CancellationToken ct) => await _repo.DeleteAllergy(id, ct);

    [HttpPost("conditions")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> AddCondition(AddConditionModel model, CancellationToken ct) => await _repo.AddCondition(model, ct);

    [HttpDelete("conditions/{id}")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> DeleteCondition(long id, CancellationToken ct) => await _repo.DeleteCondition(id, ct);
}
