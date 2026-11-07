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
            .Select(a => new AppointmentForVisit(a.Id, a.PatientId, a.DoctorId, a.BranchId, a.Status))
            .FirstOrDefaultAsync(cancellationToken);

    public Task StartVisitAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        MoveAsync(appointmentId, AppointmentStatusEnum.InProgress, "Visit opened", cancellationToken);

    public Task CompleteVisitAsync(long appointmentId, CancellationToken cancellationToken = default) =>
        MoveAsync(appointmentId, AppointmentStatusEnum.Completed, "Visit completed", cancellationToken);

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
