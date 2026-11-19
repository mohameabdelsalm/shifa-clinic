using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Billing;
using Core.DTO.Scheduling;
using Core.Entities.Scheduling;
using Core.Enums.Scheduling;
using Core.Interfaces.Billing;
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
/// Appointments for staff. Every appointment belongs to a branch, so branch scope applies to every read and change;
/// an appointment outside the caller's branches is "not found". Booking and rescheduling go through <see cref="BookingService"/>.
/// </summary>
public class AppointmentRepo : IAppointmentRepo
{
    private readonly SchedulingDbContext _context;
    private readonly IMapper _mapper;
    private readonly BookingService _booking;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly IBillingApi _billing;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public AppointmentRepo(SchedulingDbContext context, IMapper mapper, BookingService booking, IPatientsApi patients, IAccessContext access,
        IBillingApi billing, IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _booking = booking;
        _patients = patients;
        _access = access;
        _billing = billing;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> GetAvailability(AvailabilityModel model, CancellationToken cancellationToken)
    {
        EnsureBranchInScope(model.BranchId);
        var slots = await _booking.GetAvailabilityAsync(model.BranchId, model.DoctorId, model.ServiceId, model.Date, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: _mapper.Map<List<AvailableSlotDto>>(slots));
    }

    public async Task<Response> Book(BookAppointmentModel model, string? idempotencyKey, CancellationToken cancellationToken)
    {
        EnsureBranchInScope(model.BranchId);
        if (!await _patients.PatientExistsAsync(model.PatientId, cancellationToken))
            throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.BadRequest);

        var id = await _booking.BookAsync(
            new BookingRequest(model.PatientId, model.BranchId, model.DoctorId, model.ServiceId, model.StartAt, model.Source),
            idempotencyKey, cancellationToken);

        await _audit.LogAsync("Appointments.Booked", "Appointment", id, cancellationToken: cancellationToken);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: id);
    }

    public async Task<Response> Search(SearchAppointmentsModel model, CancellationToken cancellationToken)
    {
        var query = SortAppointments(InScope(_context.Appointments.AsNoTracking()).Where(FilterAppointments(model)), model);

        var paged = await query.ProjectTo<AppointmentDto>(_mapper.ConfigurationProvider).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    private static Expression<Func<Appointment, bool>> FilterAppointments(SearchAppointmentsModel model) =>
        a => a.LocalDate >= model.FromDate && a.LocalDate <= model.ToDate
             && (model.BranchId == null || a.BranchId == model.BranchId)
             && (model.DoctorId == null || a.DoctorId == model.DoctorId)
             && (model.PatientId == null || a.PatientId == model.PatientId)
             && (model.Status == null || a.Status == model.Status);

    private static IQueryable<Appointment> SortAppointments(IQueryable<Appointment> query, SearchAppointmentsModel model) => model.SortBy switch
    {
        "status" => query.OrderBy(model.SortType, a => a.Status).ThenBy(a => a.StartAt).ThenBy(a => a.Id),
        "branch" => query.OrderBy(model.SortType, a => a.BranchId).ThenBy(a => a.StartAt).ThenBy(a => a.Id),
        "doctor" => query.OrderBy(model.SortType, a => a.DoctorId).ThenBy(a => a.StartAt).ThenBy(a => a.Id),
        "queueNumber" => query.OrderBy(model.SortType, a => a.QueueNumber).ThenBy(a => a.Id),
        "date" => query.OrderBy(model.SortType, a => a.CreationDate).ThenBy(a => a.Id),
        _ => query.OrderBy(model.SortType, a => a.StartAt).ThenBy(a => a.Id),
    };

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var appointment = await InScope(_context.Appointments.AsNoTracking()).Where(a => a.Id == id).ProjectTo<AppointmentDto>(_mapper.ConfigurationProvider).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: appointment);
    }

    public async Task<Response> Confirm(long id, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(id, cancellationToken);
        return await TransitionAsync(appointment, AppointmentStatusEnum.Confirmed, reason: null, cancellationToken);
    }

    public async Task<Response> CheckIn(long id, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(id, cancellationToken);
        if (appointment.Status == AppointmentStatusEnum.CheckedIn)
            return await CheckedInAsync(appointment, appointment.QueueNumber, cancellationToken);   // repeat = no-op (also repairs a missing invoice)

        // Open from 2 hours before the start (that may be the evening before a visit just after midnight)
        // until the end of the appointment's local day.
        var now = _time.GetUtcNow();
        var timeZone = await _booking.GetBranchTimeZoneAsync(appointment.BranchId, cancellationToken);
        if (now < appointment.StartAt - SchedulingRules.CheckInOpensBefore || AvailabilityCalculator.LocalDateOf(now, timeZone) > appointment.LocalDate)
            throw new BusinessException(_localizer["CheckInNotAllowedNow"].Value, HttpStatusCode.BadRequest);

        var from = appointment.Status;
        if (appointment.MoveTo(AppointmentStatusEnum.CheckedIn) == TransitionResult.NotAllowed)
            throw InvalidTransition();

        appointment.CheckedInAt = now;
        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory { AppointmentId = appointment.Id, FromStatus = from, ToStatus = appointment.Status });

        // Queue number per branch, doctor and local day: max + 1. Check-ins of the same queue take their number one at a
        // time (ids are unique across tenants, so the lock name needs no tenant); the unique index is the final guard.
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.AcquireAsync(
                $"scheduling:queue:{appointment.BranchId}:{appointment.DoctorId}:{appointment.LocalDate:yyyyMMdd}", cancellationToken);
            appointment.QueueNumber = (await _context.Appointments
                .Where(a => a.BranchId == appointment.BranchId && a.DoctorId == appointment.DoctorId && a.LocalDate == appointment.LocalDate)
                .MaxAsync(a => a.QueueNumber, cancellationToken) ?? 0) + 1;
            _context.AddEvent(AppointmentEvents.CheckedIn(appointment), now);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Changed by someone else since we read it. A double-click check-in that lost the race is still a no-op;
            // anything else (e.g. cancelled meanwhile) is a real conflict (409).
            _context.ChangeTracker.Clear();
            var current = await _context.Appointments.AsNoTracking().FirstAsync(a => a.Id == id, cancellationToken);
            if (current.Status != AppointmentStatusEnum.CheckedIn)
                throw;
            return await CheckedInAsync(current, current.QueueNumber, cancellationToken);
        }

        await _audit.LogAsync("Appointments.CheckedIn", "Appointment", appointment.Id, cancellationToken: cancellationToken);
        return await CheckedInAsync(appointment, appointment.QueueNumber, cancellationToken);
    }

    /// <summary>
    /// Ensures the visit invoice (ADR-011: issued at check-in) and returns the queue number with it. Idempotent, so a
    /// repeated check-in repairs an invoice that a crash after the check-in left missing.
    /// </summary>
    private async Task<Response> CheckedInAsync(Appointment appointment, int? queueNumber, CancellationToken cancellationToken)
    {
        var invoiceId = await _billing.EnsureVisitInvoiceAsync(
            new VisitInvoiceRequest(appointment.Id, appointment.PatientId, appointment.BranchId, appointment.ServiceId), cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK, data: new CheckInResultDto { QueueNumber = queueNumber, InvoiceId = invoiceId });
    }

    public async Task<Response> Cancel(long id, CancelAppointmentModel model, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(id, cancellationToken);
        return await CancelAsync(appointment, model.Reason, cancellationToken);
    }

    public async Task<Response> MarkNoShow(long id, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(id, cancellationToken);
        if (appointment.Status != AppointmentStatusEnum.NoShow && _time.GetUtcNow() < appointment.StartAt + SchedulingRules.NoShowAfter)
            throw new BusinessException(_localizer["NoShowTooEarly"].Value, HttpStatusCode.BadRequest);

        // The patient didn't come, so the doctor's time is free again (availability already ignores no-shows).
        return await TransitionAsync(appointment, AppointmentStatusEnum.NoShow, reason: null, cancellationToken, releaseTicks: true);
    }

    public async Task<Response> Reschedule(RescheduleAppointmentModel model, CancellationToken cancellationToken)
    {
        var appointment = await LoadAsync(model.Id, cancellationToken);
        await _booking.RescheduleAsync(appointment, model.StartAt, cancellationToken);
        await _audit.LogAsync("Appointments.Rescheduled", "Appointment", appointment.Id, cancellationToken: cancellationToken);
        return Ok();
    }

    /// <summary>Shared with the portal: cancelling releases the appointment's ticks in the same save.</summary>
    internal async Task<Response> CancelAsync(Appointment appointment, string? reason, CancellationToken cancellationToken)
    {
        var from = appointment.Status;
        var result = appointment.MoveTo(AppointmentStatusEnum.Cancelled);
        if (result == TransitionResult.NotAllowed)
            throw InvalidTransition();
        if (result == TransitionResult.Unchanged)
            return Ok();

        appointment.CancelledAt = _time.GetUtcNow();
        appointment.CancelReason = reason?.Trim();
        await ReleaseTicksAsync(appointment, cancellationToken);
        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory { AppointmentId = appointment.Id, FromStatus = from, ToStatus = appointment.Status, Reason = reason });
        _context.AddEvent(AppointmentEvents.Cancelled(appointment), _time.GetUtcNow());

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Appointments.Cancelled", "Appointment", appointment.Id, cancellationToken: cancellationToken);
        return Ok();
    }

    private async Task<Response> TransitionAsync(Appointment appointment, AppointmentStatusEnum target, string? reason,
        CancellationToken cancellationToken, bool releaseTicks = false)
    {
        var from = appointment.Status;
        var result = appointment.MoveTo(target);
        if (result == TransitionResult.NotAllowed)
            throw InvalidTransition();
        if (result == TransitionResult.Unchanged)
            return Ok();

        if (releaseTicks)
            await ReleaseTicksAsync(appointment, cancellationToken);
        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory { AppointmentId = appointment.Id, FromStatus = from, ToStatus = target, Reason = reason });
        if (target == AppointmentStatusEnum.Confirmed)
            _context.AddEvent(AppointmentEvents.Confirmed(appointment), _time.GetUtcNow());
        else if (target == AppointmentStatusEnum.NoShow)
            _context.AddEvent(AppointmentEvents.NoShow(appointment, automatic: false), _time.GetUtcNow());
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync($"Appointments.{target}", "Appointment", appointment.Id, cancellationToken: cancellationToken);
        return Ok();
    }

    /// <summary>Soft-deletes the appointment's ticks in the same save as its status change, so the time can be booked again.</summary>
    private async Task ReleaseTicksAsync(Appointment appointment, CancellationToken cancellationToken) =>
        _context.AppointmentSlots.RemoveRange(await _context.AppointmentSlots.Where(s => s.AppointmentId == appointment.Id).ToListAsync(cancellationToken));

    private async Task<Appointment> LoadAsync(long id, CancellationToken cancellationToken) =>
        await InScope(_context.Appointments).FirstOrDefaultAsync(a => a.Id == id, cancellationToken) ?? throw NotFound();

    private IQueryable<Appointment> InScope(IQueryable<Appointment> appointments)
    {
        if (_access.HasAllBranches)
            return appointments;

        var scope = _access.BranchIds.ToList();
        return appointments.Where(a => scope.Contains(a.BranchId));
    }

    private void EnsureBranchInScope(long branchId)
    {
        if (!_access.CanAccessBranch(branchId))
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);
    }

    private Response Ok() => new(_localizer["Updated"].Value, HttpStatusCode.OK);

    private BusinessException InvalidTransition() => new(_localizer["InvalidTransition"].Value, HttpStatusCode.BadRequest);

    private BusinessException NotFound() => new(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);
}
