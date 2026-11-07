using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Scheduling;
using Core.DTO.Patients;
using Core.Enums.Scheduling;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Interfaces.Scheduling;
using Core.Models.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Scheduling;

/// <summary>
/// Appointments for a patient or guardian signed in with a phone OTP. They see and book only for patients linked to
/// their verified phone; booking and cancelling also need <c>CanBook</c> on that link. Anything else is "not found".
/// </summary>
public class PortalAppointmentRepo : IPortalAppointmentRepo
{
    private readonly SchedulingDbContext _context;
    private readonly IMapper _mapper;
    private readonly BookingService _booking;
    private readonly AppointmentRepo _appointments;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PortalAppointmentRepo(SchedulingDbContext context, IMapper mapper, BookingService booking, AppointmentRepo appointments, IPatientsApi patients,
        IAccessContext access, IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _booking = booking;
        _appointments = appointments;
        _patients = patients;
        _access = access;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> GetAvailability(AvailabilityModel model, CancellationToken cancellationToken)
    {
        var slots = await _booking.GetAvailabilityAsync(model.BranchId, model.DoctorId, model.ServiceId, model.Date, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: _mapper.Map<List<AvailableSlotDto>>(slots));
    }

    public async Task<Response> Book(PortalBookAppointmentModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var linked = await LinkedAsync(cancellationToken);
        if (!linked.Any(p => p.PatientId == model.PatientId && p.CanBook))
            throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);

        var id = await _booking.BookAsync(
            new BookingRequest(model.PatientId, model.BranchId, model.DoctorId, model.ServiceId, model.StartAt, AppointmentSourceEnum.Online),
            idempotencyKey, cancellationToken);

        await _audit.LogAsync("Appointments.BookedOnline", "Appointment", id, cancellationToken: cancellationToken);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: id);
    }

    public async Task<Response> GetMine(CancellationToken cancellationToken)
    {
        var patientIds = (await LinkedAsync(cancellationToken)).Select(p => p.PatientId).ToList();
        var appointments = await _context.Appointments.AsNoTracking()
            .Where(a => patientIds.Contains(a.PatientId))
            .OrderByDescending(a => a.StartAt)
            .Take(200)
            .ProjectTo<PortalAppointmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: appointments);
    }

    public async Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken cancellationToken)
    {
        var canBook = (await LinkedAsync(cancellationToken)).Where(p => p.CanBook).Select(p => p.PatientId).ToList();
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id && canBook.Contains(a.PatientId), cancellationToken)
            ?? throw new BusinessException(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);

        if (appointment.Status != AppointmentStatusEnum.Cancelled && _time.GetUtcNow() > appointment.StartAt - SchedulingRules.PortalCancelUntilBefore)
            throw new BusinessException(_localizer["CancelWindowPassed"].Value, HttpStatusCode.BadRequest);

        return await _appointments.CancelAsync(appointment, model.Reason, cancellationToken);
    }

    private Task<IReadOnlyList<PortalPatientAccess>> LinkedAsync(CancellationToken cancellationToken)
    {
        var phone = _access.PortalPhoneNumber ?? throw new InvalidOperationException("Not a portal request.");
        return _patients.GetPortalPatientsAsync(phone, cancellationToken);
    }
}
