using Core.Common;
using Core.DTO.Tenancy;
using Core.Interfaces.Scheduling;
using Core.Models.Scheduling;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Shifa.Controllers.Scheduling;

/// <summary>Booking for patients and guardians signed in with a phone OTP.</summary>
[ApiController]
[PortalUser]
[RequireFeature(FeatureCodes.PatientPortal)]
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
