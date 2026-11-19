using Core.Common;
using Core.DTO.Reporting;
using Core.DTO.Tenancy;
using Core.Interfaces.Reporting;
using Core.Models.Reporting;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Reporting;

/// <summary>
/// Reports (ADR-016). Body "format": Json (default, the usual Response), Xlsx or Pdf (a file download). Reads only, so
/// they stay available while a clinic is read-only (A6).
/// </summary>
[ApiController]
[RequireFeature(FeatureCodes.Reports)]
[Permission(Permissions.ReportsRead)]
[ReadOperation]
[Route("api/reports")]
public class ReportController : ControllerBase
{
    private readonly IReportRepo _repo;

    public ReportController(IReportRepo repo)
    {
        _repo = repo;
    }

    /// <summary>Per local day and branch: appointments, no-show rate, revenue and collections.</summary>
    [HttpPost("daily-branches")]
    public async Task<IActionResult> DailyBranches(ReportModel model, CancellationToken ct) => ToResult(await _repo.DailyBranches(model, ct));

    /// <summary>Per local day: invoiced, credited, revenue, payments by method, refunds, collections and the difference.</summary>
    [HttpPost("revenue")]
    public async Task<IActionResult> Revenue(ReportModel model, CancellationToken ct) => ToResult(await _repo.Revenue(model, ct));

    /// <summary>Per doctor: appointments, no-show rate and utilization.</summary>
    [HttpPost("doctors")]
    public async Task<IActionResult> Doctors(ReportModel model, CancellationToken ct) => ToResult(await _repo.Doctors(model, ct));

    private IActionResult ToResult(ReportOutput output) =>
        output.Content is { } content ? File(content, output.ContentType, output.FileName) : Ok(output.Json);
}
