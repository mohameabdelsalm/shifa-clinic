using Microsoft.AspNetCore.Mvc;
using Shifa.Core.Common;
using Shifa.Infrastructure.Auth;
using Shifa.Scheduling.Interfaces;
using Shifa.Scheduling.Models;

namespace Shifa.Scheduling.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    public const string IdempotencyHeader = "Idempotency-Key";

    private readonly IAppointmentRepo _repo;

    public AppointmentController(IAppointmentRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("availability")]
    [Permission(Permissions.AppointmentsRead)]
    public async Task<Response> Availability(AvailabilityModel model, CancellationToken ct) => await _repo.GetAvailability(model, ct);

    [HttpPost]
    [Permission(Permissions.AppointmentsCreate)]
    public async Task<Response> Book(BookAppointmentModel model, [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey, CancellationToken ct) =>
        await _repo.Book(model, idempotencyKey, ct);

    [HttpPost("search")]
    [Permission(Permissions.AppointmentsRead)]
    public async Task<Response> Search(SearchAppointmentsModel model, CancellationToken ct) => await _repo.Search(model, ct);

    [HttpGet("{id}")]
    [Permission(Permissions.AppointmentsRead)]
    public async Task<Response> View(long id, CancellationToken ct) => await _repo.View(id, ct);

    [HttpPut("{id}/confirm")]
    [Permission(Permissions.AppointmentsCreate)]
    public async Task<Response> Confirm(long id, CancellationToken ct) => await _repo.Confirm(id, ct);

    [HttpPut("{id}/check-in")]
    [Permission(Permissions.AppointmentsCheckIn)]
    public async Task<Response> CheckIn(long id, CancellationToken ct) => await _repo.CheckIn(id, ct);

    [HttpPut("{id}/cancel")]
    [Permission(Permissions.AppointmentsCancel)]
    public async Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken ct) => await _repo.Cancel(id, model, ct);

    [HttpPut("{id}/no-show")]
    [Permission(Permissions.AppointmentsCheckIn)]
    public async Task<Response> NoShow(long id, CancellationToken ct) => await _repo.MarkNoShow(id, ct);

    [HttpPut("reschedule")]
    [Permission(Permissions.AppointmentsCreate)]
    public async Task<Response> Reschedule(RescheduleAppointmentModel model, CancellationToken ct) => await _repo.Reschedule(model, ct);
}

/// <summary>Booking for patients and guardians signed in with a phone OTP.</summary>
[ApiController]
[PortalUser]
[Route("api/portal/appointments")]
public class PortalAppointmentController : ControllerBase
{
    private readonly IPortalAppointmentRepo _repo;

    public PortalAppointmentController(IPortalAppointmentRepo repo)
    {
        _repo = repo;
    }

    [HttpPost("availability")]
    public async Task<Response> Availability(AvailabilityModel model, CancellationToken ct) => await _repo.GetAvailability(model, ct);

    [HttpPost]
    public async Task<Response> Book(PortalBookAppointmentModel model,
        [FromHeader(Name = AppointmentController.IdempotencyHeader)] string? idempotencyKey, CancellationToken ct) =>
        await _repo.Book(model, idempotencyKey, ct);

    [HttpGet]
    public async Task<Response> GetMine(CancellationToken ct) => await _repo.GetMine(ct);

    [HttpPut("{id}/cancel")]
    public async Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken ct) => await _repo.Cancel(id, model, ct);
}
