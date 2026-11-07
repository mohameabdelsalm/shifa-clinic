using Core.Common;
using Core.Interfaces.Clinical;
using Core.Models.Clinical;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Clinical;

[ApiController]
[Route("api/visits")]
public class VisitController : ControllerBase
{
    private const long MaxUploadBytes = 11 * 1024 * 1024;   // 10 MB file + multipart overhead

    private readonly IVisitRepo _repo;

    public VisitController(IVisitRepo repo)
    {
        _repo = repo;
    }

    /// <summary>Opens the visit of a checked-in appointment (the appointment's doctor only). Opening again returns the same visit.</summary>
    [HttpPost]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> Open(OpenVisitModel model, CancellationToken ct) => await _repo.Open(model, ct);

    [HttpPost("search")]
    [Permission(Permissions.ClinicalRead)]
    public async Task<Response> Search(SearchVisitsModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.ClinicalRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPut("note")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> SaveNote(SaveClinicalNoteModel model, CancellationToken ct) => await _repo.SaveNote(model, ct);

    [HttpPost("vitals")]
    [Permission(Permissions.ClinicalVitals)]
    public async Task<Response> AddVitals(AddVitalSignsModel model, CancellationToken ct) => await _repo.AddVitals(model, ct);

    [HttpPut("diagnoses")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> SetDiagnoses(SetDiagnosesModel model, CancellationToken ct) => await _repo.SetDiagnoses(model, ct);

    [HttpPut("prescription")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> SetPrescription(SetPrescriptionModel model, CancellationToken ct) => await _repo.SetPrescription(model, ct);

    /// <summary>Attaches a PDF, JPEG or PNG (up to 10 MB) to an open visit.</summary>
    [HttpPost("{id}/attachments")]
    [Permission(Permissions.ClinicalWrite)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    public async Task<Response> AddAttachment(long id, IFormFile file, CancellationToken ct) => await _repo.AddAttachment(id, file, ct);

    [HttpGet("attachments/{attachmentId}")]
    [Permission(Permissions.ClinicalRead)]
    public async Task<IActionResult> DownloadAttachment(long attachmentId, CancellationToken ct)
    {
        var attachment = await _repo.DownloadAttachment(attachmentId, ct);
        return File(attachment.Content, attachment.ContentType, attachment.FileName);
    }

    [HttpPut("{id}/complete")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> Complete(long id, CancellationToken ct) => await _repo.Complete(id, ct);

    /// <summary>Adds a correction to a completed visit (completed visits never change).</summary>
    [HttpPost("addenda")]
    [Permission(Permissions.ClinicalWrite)]
    public async Task<Response> AddAddendum(AddAddendumModel model, CancellationToken ct) => await _repo.AddAddendum(model, ct);
}
