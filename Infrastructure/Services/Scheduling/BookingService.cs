using System.Net;
using System.Security.Cryptography;
using System.Text;
using Core;
using Core.Common;
using Core.DTO.Clinic;
using Core.DTO.Scheduling;
using Core.Entities.Scheduling;
using Core.Enums.Scheduling;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Infrastructure.Configurations.Scheduling;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Services.Scheduling;

/// <summary>
/// The booking transaction of ADR-008, shared by the staff and portal repos (which check scope and patient access first).
/// <list type="bullet">
/// <item>The requested start must be one of the computed available starts.</item>
/// <item>Appointment + 5-minute ticks + status history + idempotency record go in one SaveChanges; the unique key on the
/// ticks decides between concurrent requests (loser → 409).</item>
/// <item>Rescheduling releases the old ticks and claims new ones inside one explicit transaction.</item>
/// </list>
/// </summary>
public sealed class BookingService
{
    public const string BookOperation = "BookAppointment";

    private readonly SchedulingDbContext _context;
    private readonly IClinicApi _clinic;
    private readonly IAccessContext _access;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public BookingService(SchedulingDbContext context, IClinicApi clinic, IAccessContext access, TimeProvider time,
        IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _clinic = clinic;
        _access = access;
        _time = time;
        _localizer = localizer;
    }

    public async Task<IReadOnlyList<AvailableSlot>> GetAvailabilityAsync(long branchId, long doctorId, long serviceId, DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var (inputs, timeZone) = await LoadInputsAsync(branchId, doctorId, serviceId, cancellationToken);
        return await ComputeAsync(inputs, timeZone, localDate, excludeAppointmentId: null, cancellationToken);
    }

    /// <returns>The appointment id (the original one when the request is a retry with the same Idempotency-Key).</returns>
    public async Task<long> BookAsync(BookingRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 100)
            throw Fail("IdempotencyKeyRequired", HttpStatusCode.BadRequest);

        var key = idempotencyKey.Trim();
        var operation = OperationForCaller();
        var requestHash = Hash(request);
        var now = _time.GetUtcNow();

        var replay = await FindReplayAsync(operation, key, requestHash, now, cancellationToken);
        if (replay is not null)
            return replay.Value;

        var (inputs, timeZone) = await LoadInputsAsync(request.BranchId, request.DoctorId, request.ServiceId, cancellationToken);
        var startUtc = request.StartAt.ToUniversalTime();
        var localDate = AvailabilityCalculator.LocalDateOf(startUtc, timeZone);

        // If a concurrent request with the same key committed after the replay check above, its appointment now makes
        // the slot or the day look taken; answer like a retry in that case instead of failing.
        var slots = await ComputeAsync(inputs, timeZone, localDate, excludeAppointmentId: null, cancellationToken);
        var slot = slots.FirstOrDefault(s => s.StartUtc == startUtc);
        if (slot is null)
            return await FindReplayAsync(operation, key, requestHash, now, cancellationToken) ?? throw Fail("SlotNotAvailable", HttpStatusCode.Conflict);

        if (await HasOtherVisitThatDayAsync(request.PatientId, request.DoctorId, localDate, ignoreAppointmentId: null, cancellationToken))
            return await FindReplayAsync(operation, key, requestHash, now, cancellationToken) ?? throw Fail("PatientAlreadyBookedThatDay", HttpStatusCode.BadRequest);

        var appointment = new Appointment
        {
            BranchId = request.BranchId,
            DoctorId = request.DoctorId,
            ServiceId = request.ServiceId,
            PatientId = request.PatientId,
            StartAt = slot.StartUtc,
            EndAt = slot.EndUtc,
            LocalDate = localDate,
            Source = request.Source,
        };
        appointment.Book(await InitialStatusAsync(request, cancellationToken));

        _context.Appointments.Add(appointment);
        ClaimTicks(appointment);
        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory { AppointmentId = appointment.Id, ToStatus = appointment.Status });
        _context.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Operation = operation,
            Key = key,
            RequestHash = requestHash,
            ResultId = appointment.Id,
            ExpiresAt = now + SchedulingRules.IdempotencyLifetime,
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return appointment.Id;
        }
        catch (DbUpdateException ex)
        {
            _context.ChangeTracker.Clear();

            // A concurrent request with the same key may have committed first. Its ticks collide with ours before the
            // idempotency row does, so check for a replay on any of our unique violations before mapping the error.
            if (IsUniqueViolation(ex, SchedulingIndexes.SlotTick) || IsUniqueViolation(ex, SchedulingIndexes.PatientDoctorDay)
                || IsUniqueViolation(ex, SchedulingIndexes.Idempotency))
            {
                var replayed = await FindReplayAsync(operation, key, requestHash, now, cancellationToken);
                if (replayed is not null)
                    return replayed.Value;
            }

            if (IsUniqueViolation(ex, SchedulingIndexes.SlotTick))
                throw Fail("SlotTaken", HttpStatusCode.Conflict);
            if (IsUniqueViolation(ex, SchedulingIndexes.PatientDoctorDay))
                throw Fail("PatientAlreadyBookedThatDay", HttpStatusCode.BadRequest);
            if (IsUniqueViolation(ex, SchedulingIndexes.Idempotency))
                throw Fail("IdempotencyKeyReused", HttpStatusCode.UnprocessableEntity);

            throw;
        }
    }

    /// <summary>Moves a Pending/Confirmed appointment to another available start of the same doctor, branch and service.</summary>
    public async Task RescheduleAsync(Appointment appointment, DateTimeOffset newStart, CancellationToken cancellationToken)
    {
        if (appointment.Status is not (AppointmentStatusEnum.Pending or AppointmentStatusEnum.Confirmed))
            throw Fail("InvalidTransition", HttpStatusCode.BadRequest);

        var (inputs, timeZone) = await LoadInputsAsync(appointment.BranchId, appointment.DoctorId, appointment.ServiceId, cancellationToken);
        var startUtc = newStart.ToUniversalTime();
        var localDate = AvailabilityCalculator.LocalDateOf(startUtc, timeZone);

        var slots = await ComputeAsync(inputs, timeZone, localDate, excludeAppointmentId: appointment.Id, cancellationToken);
        var slot = slots.FirstOrDefault(s => s.StartUtc == startUtc) ?? throw Fail("SlotNotAvailable", HttpStatusCode.Conflict);

        if (await HasOtherVisitThatDayAsync(appointment.PatientId, appointment.DoctorId, localDate, appointment.Id, cancellationToken))
            throw Fail("PatientAlreadyBookedThatDay", HttpStatusCode.BadRequest);

        var previousStart = appointment.StartAt;
        try
        {
            // Release first, then claim: the new time may overlap the appointment's own old ticks.
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            _context.AppointmentSlots.RemoveRange(await _context.AppointmentSlots.Where(s => s.AppointmentId == appointment.Id).ToListAsync(cancellationToken));
            await _context.SaveChangesAsync(cancellationToken);

            appointment.StartAt = slot.StartUtc;
            appointment.EndAt = slot.EndUtc;
            appointment.LocalDate = localDate;
            // Always UPDATE the appointment row, even when the start didn't change, so its rowversion is checked:
            // otherwise a cancel committed meanwhile would leave this cancelled appointment holding new live ticks.
            _context.Entry(appointment).Property(a => a.StartAt).IsModified = true;
            ClaimTicks(appointment);
            _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
            {
                AppointmentId = appointment.Id,
                FromStatus = appointment.Status,
                ToStatus = appointment.Status,
                Reason = $"Rescheduled from {previousStart:O}",
            });
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            if (IsUniqueViolation(ex, SchedulingIndexes.SlotTick))
                throw Fail("SlotTaken", HttpStatusCode.Conflict);
            if (IsUniqueViolation(ex, SchedulingIndexes.PatientDoctorDay))
                throw Fail("PatientAlreadyBookedThatDay", HttpStatusCode.BadRequest);
            throw;
        }
    }

    /// <summary>The branch's time zone (for check-in windows and local dates).</summary>
    public async Task<TimeZoneInfo> GetBranchTimeZoneAsync(long branchId, CancellationToken cancellationToken)
    {
        var id = await _clinic.GetBranchTimeZoneAsync(branchId, cancellationToken) ?? throw Fail("BranchNotFound", HttpStatusCode.NotFound);
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public static bool IsUniqueViolation(DbUpdateException ex, string indexName) =>
        ex.InnerException?.Message.Contains(indexName, StringComparison.Ordinal) == true;

    private async Task<(BookingInputs Inputs, TimeZoneInfo TimeZone)> LoadInputsAsync(long branchId, long doctorId, long serviceId, CancellationToken cancellationToken)
    {
        var inputs = await _clinic.GetBookingInputsAsync(branchId, doctorId, serviceId, cancellationToken)
            ?? throw Fail("BookingTargetInvalid", HttpStatusCode.BadRequest);
        return (inputs, TimeZoneInfo.FindSystemTimeZoneById(inputs.TimeZoneId));
    }

    /// <summary>Available starts: schedule minus time off minus the doctor's other appointments at any branch.</summary>
    private async Task<IReadOnlyList<AvailableSlot>> ComputeAsync(BookingInputs inputs, TimeZoneInfo timeZone, DateOnly localDate,
        long? excludeAppointmentId, CancellationToken cancellationToken)
    {
        var (dayFrom, dayTo) = AvailabilityCalculator.DayRangeUtc(localDate, timeZone);
        var busy = new List<TimeRange>(await _clinic.GetDoctorTimeOffAsync(inputs.DoctorId, dayFrom, dayTo, cancellationToken));

        var booked = await _context.Appointments.AsNoTracking()
            .Where(a => a.DoctorId == inputs.DoctorId && a.StartAt < dayTo && dayFrom < a.EndAt
                        && a.Status != AppointmentStatusEnum.Cancelled && a.Status != AppointmentStatusEnum.NoShow
                        && a.Id != excludeAppointmentId)
            .Select(a => new { a.StartAt, a.EndAt })
            .ToListAsync(cancellationToken);
        busy.AddRange(booked.Select(a => new TimeRange(a.StartAt, a.EndAt)));

        var now = _time.GetUtcNow();
        return AvailabilityCalculator.Calculate(localDate, timeZone, inputs.Schedule, inputs.ServiceDurationMinutes, busy,
            notBefore: now, notAfter: now + SchedulingRules.BookingHorizon);
    }

    /// <summary>
    /// Idempotency keys are scoped to the caller (staff membership or portal phone), so one caller's key can never
    /// block or replay another caller's request. The phone is stored hashed.
    /// </summary>
    private string OperationForCaller()
    {
        if (_access.IsPortalUser && _access.PortalPhoneNumber is { } phone)
            return $"{BookOperation}:p:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(phone)))[..16]}";
        if (_access.MembershipId is { } membershipId)
            return $"{BookOperation}:m:{membershipId}";
        throw new InvalidOperationException("Booking needs a staff membership or a portal user.");
    }

    private async Task<long?> FindReplayAsync(string operation, string key, string requestHash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await _context.IdempotencyRecords.FirstOrDefaultAsync(r => r.Operation == operation && r.Key == key, cancellationToken);
        if (record is null)
            return null;

        if (record.ExpiresAt <= now)
        {
            // Expired: forget it so the key can be used again.
            _context.IdempotencyRecords.Remove(record);
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        return record.RequestHash == requestHash ? record.ResultId : throw Fail("IdempotencyKeyReused", HttpStatusCode.UnprocessableEntity);
    }

    private Task<bool> HasOtherVisitThatDayAsync(long patientId, long doctorId, DateOnly localDate, long? ignoreAppointmentId, CancellationToken cancellationToken) =>
        _context.Appointments.AnyAsync(a => a.PatientId == patientId && a.DoctorId == doctorId && a.LocalDate == localDate
            && a.Status != AppointmentStatusEnum.Cancelled && a.Id != ignoreAppointmentId, cancellationToken);

    /// <summary>Rule 5: an online booking by a patient with 3+ no-shows waits for reception to confirm it.</summary>
    private async Task<AppointmentStatusEnum> InitialStatusAsync(BookingRequest request, CancellationToken cancellationToken)
    {
        if (request.Source != AppointmentSourceEnum.Online)
            return AppointmentStatusEnum.Confirmed;

        var noShows = await _context.Appointments.CountAsync(a => a.PatientId == request.PatientId && a.Status == AppointmentStatusEnum.NoShow, cancellationToken);
        return noShows >= SchedulingRules.NoShowsBeforePending ? AppointmentStatusEnum.Pending : AppointmentStatusEnum.Confirmed;
    }

    private void ClaimTicks(Appointment appointment)
    {
        foreach (var tick in AvailabilityCalculator.Ticks(appointment.StartAt, appointment.EndAt))
            _context.AppointmentSlots.Add(new AppointmentSlot { AppointmentId = appointment.Id, DoctorId = appointment.DoctorId, SlotStart = tick });
    }

    private static string Hash(BookingRequest r) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{r.PatientId}|{r.BranchId}|{r.DoctorId}|{r.ServiceId}|{r.StartAt.UtcTicks}|{r.Source}")));

    private BusinessException Fail(string key, HttpStatusCode status) => new(_localizer[key].Value, status);
}
