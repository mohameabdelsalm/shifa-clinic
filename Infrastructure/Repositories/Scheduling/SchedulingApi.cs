using System.Net;
using Core;
using Core.Common;
using Core.DTO.Scheduling;
using Core.Entities.Scheduling;
using Core.Enums.Scheduling;
using Core.Interfaces.Scheduling;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Scheduling;

public sealed class SchedulingApi : ISchedulingApi
{
    private readonly SchedulingDbContext _context;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public SchedulingApi(SchedulingDbContext context, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public Task<AppointmentForVisit?> GetAppointmentForVisitAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        _context.Appointments.AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentForVisit(a.Id, a.PatientId, a.DoctorId, a.BranchId, a.Status, a.ServiceId))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<AppointmentState?> GetAppointmentStateAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        _context.Appointments.AsNoTracking()
            .Where(a => a.Id == appointmentId)
            .Select(a => new AppointmentState(a.Id, a.Status, a.StartAt))
            .FirstOrDefaultAsync(cancellationToken);

    public Task StartVisitAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        MoveAsync(appointmentId, AppointmentStatusEnum.InProgress, "Visit opened", cancellationToken);

    public Task CompleteVisitAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        MoveAsync(appointmentId, AppointmentStatusEnum.Completed, "Visit completed", cancellationToken);

    public async Task<IReadOnlyList<AppointmentCountRow>> CountAppointmentsAsync(DateOnly fromLocalDate, DateOnly toLocalDate,
        IReadOnlyCollection<long>? branchIds, long? doctorId, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments.AsNoTracking().Where(a => a.LocalDate >= fromLocalDate && a.LocalDate <= toLocalDate);
        if (branchIds is not null)
        {
            var ids = branchIds.ToList();
            query = query.Where(a => ids.Contains(a.BranchId));
        }
        if (doctorId is not null)
            query = query.Where(a => a.DoctorId == doctorId);

        // Grouped in SQL: one row per day × branch × doctor × status, never one row per appointment.
        var rows = await query
            .GroupBy(a => new { a.LocalDate, a.BranchId, a.DoctorId, a.Status })
            .Select(g => new
            {
                g.Key.LocalDate, g.Key.BranchId, g.Key.DoctorId, g.Key.Status, Count = g.Count(),
                Minutes = g.Sum(a => EF.Functions.DateDiffMinute(a.StartAt, a.EndAt)),
            })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new AppointmentCountRow(r.LocalDate, r.BranchId, r.DoctorId, r.Status, r.Count, r.Minutes)).ToList();
    }

    /// <summary>One status change with its history row; the appointment's rowversion makes a concurrent change fail (409).</summary>
    private async Task MoveAsync(long appointmentId, AppointmentStatusEnum target, string reason, CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new BusinessException(_localizer["AppointmentNotFound"].Value, HttpStatusCode.NotFound);

        var from = appointment.Status;
        switch (appointment.MoveTo(target))
        {
            case TransitionResult.Unchanged:
                return;
            case TransitionResult.NotAllowed:
                throw new BusinessException(_localizer["AppointmentNotReadyForVisit"].Value, HttpStatusCode.BadRequest);
        }

        _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory { AppointmentId = appointment.Id, FromStatus = from, ToStatus = target, Reason = reason });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
