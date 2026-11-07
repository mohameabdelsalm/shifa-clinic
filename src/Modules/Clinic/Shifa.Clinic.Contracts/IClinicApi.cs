namespace Shifa.Clinic.Contracts;

/// <summary>A weekly working window of a doctor at one branch, in the branch's local time.</summary>
public sealed record ScheduleWindow(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int SlotMinutes);

/// <summary>Everything Scheduling needs to compute availability and validate a booking (ADR-008).</summary>
public sealed record BookingInputs(
    long BranchId,
    string TimeZoneId,
    long DoctorId,
    long ServiceId,
    int ServiceDurationMinutes,
    IReadOnlyList<ScheduleWindow> Schedule);

public sealed record TimeRange(DateTimeOffset From, DateTimeOffset To);

public interface IClinicApi
{
    /// <summary>
    /// The ids of <paramref name="branchIds"/> that exist and are active in the current tenant.
    /// Other modules use this to validate branch ids they store (no cross-schema foreign keys).
    /// </summary>
    Task<IReadOnlyList<long>> GetExistingBranchIdsAsync(IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Booking inputs for a doctor at a branch for a service, or null when the combination can't be booked:
    /// branch or doctor or service missing/inactive, doctor not working at the branch, or the service's specialty
    /// not among the doctor's specialties. Runs in the caller's tenant context.
    /// </summary>
    Task<BookingInputs?> GetBookingInputsAsync(long branchId, long doctorId, long serviceId, CancellationToken cancellationToken = default);

    /// <summary>The IANA time zone of an existing branch (active or not), or null.</summary>
    Task<string?> GetBranchTimeZoneAsync(long branchId, CancellationToken cancellationToken = default);

    /// <summary>The doctor's time off overlapping [fromUtc, toUtc).</summary>
    Task<IReadOnlyList<TimeRange>> GetDoctorTimeOffAsync(long doctorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
