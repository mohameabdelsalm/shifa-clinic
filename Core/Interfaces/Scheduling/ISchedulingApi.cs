using Core.DTO.Scheduling;

namespace Core.Interfaces.Scheduling;

/// <summary>What other modules may ask Scheduling (ADR-010). Appointment state stays owned by Scheduling.</summary>
public interface ISchedulingApi
{
    /// <summary>The appointment in the current tenant, or null.</summary>
    Task<AppointmentForVisit?> GetAppointmentForVisitAsync(long appointmentId, CancellationToken cancellationToken = default);

    /// <summary>CheckedIn → InProgress when the doctor opens the visit. Already InProgress = no-op; any other state = 400.</summary>
    Task StartVisitAsync(long appointmentId, CancellationToken cancellationToken = default);

    /// <summary>InProgress → Completed when the visit is completed. Already Completed = no-op; any other state = 400.</summary>
    Task CompleteVisitAsync(long appointmentId, CancellationToken cancellationToken = default);
}
