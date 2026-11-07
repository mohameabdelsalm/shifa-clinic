using Core.Common;
using Core.Interfaces.Scheduling;
using Core.Models.Scheduling;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Scheduling;

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
