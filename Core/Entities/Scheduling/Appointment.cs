using Core.Entities.Common;
using Core.Enums.Scheduling;

namespace Core.Entities.Scheduling;

/// <summary>
/// A booked visit. Branch, doctor, service and patient are ids of other modules (no cross-schema keys).
/// The appointment owns its <see cref="AppointmentSlot"/> ticks; they are what prevents double booking (ADR-008).
/// </summary>
public class Appointment : TenantOwnedEntity
{
    private static readonly Dictionary<AppointmentStatusEnum, AppointmentStatusEnum[]> AllowedTransitions = new()
    {
        [AppointmentStatusEnum.Pending] = [AppointmentStatusEnum.Confirmed, AppointmentStatusEnum.Cancelled],
        [AppointmentStatusEnum.Confirmed] = [AppointmentStatusEnum.CheckedIn, AppointmentStatusEnum.Cancelled, AppointmentStatusEnum.NoShow],
        [AppointmentStatusEnum.CheckedIn] = [AppointmentStatusEnum.InProgress],
        [AppointmentStatusEnum.InProgress] = [AppointmentStatusEnum.Completed],
        [AppointmentStatusEnum.Completed] = [],
        [AppointmentStatusEnum.Cancelled] = [],
        [AppointmentStatusEnum.NoShow] = [],
    };

    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }
    public long PatientId { get; set; }

    /// <summary>UTC.</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>UTC. StartAt + the service duration rounded up to whole schedule slots.</summary>
    public DateTimeOffset EndAt { get; set; }

    /// <summary>The day of the appointment in the branch's time zone (queue numbers and the one-per-day rule use it).</summary>
    public DateOnly LocalDate { get; set; }

    public AppointmentStatusEnum Status { get; private set; } = AppointmentStatusEnum.Confirmed;
    public AppointmentSourceEnum Source { get; set; }
    public int? QueueNumber { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Optimistic concurrency: two changes to the same appointment can't both win.</summary>
    public byte[] RowVersion { get; set; } = [];

    /// <summary>Sets the status of a new appointment (Pending or Confirmed).</summary>
    public void Book(AppointmentStatusEnum initialStatus)
    {
        if (initialStatus is not (AppointmentStatusEnum.Pending or AppointmentStatusEnum.Confirmed))
            throw new ArgumentOutOfRangeException(nameof(initialStatus));
        Status = initialStatus;
    }

    /// <summary>Moves to <paramref name="target"/> when the lifecycle allows it (docs/vision.md).</summary>
    public TransitionResult MoveTo(AppointmentStatusEnum target)
    {
        if (Status == target)
            return TransitionResult.Unchanged;
        if (!AllowedTransitions[Status].Contains(target))
            return TransitionResult.NotAllowed;

        Status = target;
        return TransitionResult.Changed;
    }

    public bool IsActive => Status is not (AppointmentStatusEnum.Cancelled or AppointmentStatusEnum.NoShow);
}
