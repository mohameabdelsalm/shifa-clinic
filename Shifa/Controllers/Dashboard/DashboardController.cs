using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Dashboard;
using Core.Models.Dashboard;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Dashboard;

/// <summary>Clinic statistics for a period (Trio HRM dashboard pattern): appointments, patients, revenue and collections.</summary>
[ApiController]
[RequireFeature(FeatureCodes.Dashboard)]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepo _repo;

    public DashboardController(IDashboardRepo repo)
    {
        _repo = repo;
    }

    /// <summary>Appointments, completed visits, cancellations, no-show rate and new patients, vs the same period last year.</summary>
    [HttpPost("statistics")]
    [Permission(Permissions.ReportsRead)]
    public async Task<Response> Statistics(DashboardModel model, CancellationToken ct) => await _repo.Statistics(model, ct);

    /// <summary>The same numbers per day (periods up to 31 days) or per month.</summary>
    [HttpPost("timeline")]
    [Permission(Permissions.ReportsRead)]
    public async Task<Response> Timeline(DashboardModel model, CancellationToken ct) => await _repo.Timeline(model, ct);

    /// <summary>Totals per branch.</summary>
    [HttpPost("branches")]
    [Permission(Permissions.ReportsRead)]
    public async Task<Response> Branches(DashboardModel model, CancellationToken ct) => await _repo.Branches(model, ct);

    /// <summary>Totals per doctor.</summary>
    [HttpPost("doctors")]
    [Permission(Permissions.ReportsRead)]
    public async Task<Response> Doctors(DashboardModel model, CancellationToken ct) => await _repo.Doctors(model, ct);

    /// <summary>Collections per payment method (cash, card, wallet, online), refunds and net, per day or month.</summary>
    [HttpPost("payments")]
    [Permission(Permissions.ReportsRead)]
    public async Task<Response> Payments(PaymentsDashboardModel model, CancellationToken ct) => await _repo.Payments(model, ct);
}
