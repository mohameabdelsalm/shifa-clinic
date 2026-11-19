using Core.Entities.Scheduling;
using Core.Enums.Scheduling;
using Core.Interfaces.Common;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Scheduling;

/// <summary>
/// A10: a confirmed appointment not checked in 30 minutes after its start becomes NoShow (runs in the tenant's scope,
/// called by the no-show job). Each appointment is saved on its own; one that changed meanwhile (checked in, cancelled)
/// loses the rowversion race and is left alone.
/// </summary>
public sealed class NoShowMarker
{
    private readonly SchedulingDbContext _context;
    private readonly ITenancyApi _tenancy;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;

    public NoShowMarker(SchedulingDbContext context, ITenancyApi tenancy, ITenantContext tenant, IAuditService audit, TimeProvider time)
    {
        _context = context;
        _tenancy = tenancy;
        _tenant = tenant;
        _audit = audit;
        _time = time;
    }

    public async Task<int> MarkAsync(IReadOnlyCollection<long> appointmentIds, CancellationToken cancellationToken)
    {
        var tenant = await _tenancy.GetTenantAsync(_tenant.TenantId ?? throw new InvalidOperationException("No tenant."), cancellationToken);
        if (tenant is not { IsActive: true })
            return 0;

        var marked = 0;
        foreach (var id in appointmentIds)
        {
            var now = _time.GetUtcNow();
            var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            if (appointment is null || appointment.Status != AppointmentStatusEnum.Confirmed || now < appointment.StartAt + SchedulingRules.NoShowAfter)
                continue;

            appointment.MoveTo(AppointmentStatusEnum.NoShow);
            // The doctor's time is free again, as with a manual no-show.
            _context.AppointmentSlots.RemoveRange(await _context.AppointmentSlots.Where(s => s.AppointmentId == id).ToListAsync(cancellationToken));
            _context.AppointmentStatusHistory.Add(new AppointmentStatusHistory
            {
                AppointmentId = id,
                FromStatus = AppointmentStatusEnum.Confirmed,
                ToStatus = AppointmentStatusEnum.NoShow,
                Reason = "Automatic: not checked in 30 minutes after the start",
            });
            _context.AddEvent(AppointmentEvents.NoShow(appointment, automatic: true), now);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                marked++;
                await _audit.LogAsync("Appointments.NoShow", "Appointment", id, detail: "automatic", cancellationToken: CancellationToken.None);
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.ChangeTracker.Clear();
            }
        }

        return marked;
    }
}
