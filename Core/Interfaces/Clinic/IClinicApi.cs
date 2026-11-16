using Core.DTO.Clinic;

namespace Core.Interfaces.Clinic;

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

    /// <summary>The service's current price at the branch (null when the service is inactive or has no open price there).</summary>
    Task<ServicePriceSnapshot?> GetCurrentPriceAsync(long serviceId, long branchId, CancellationToken cancellationToken = default);

    /// <summary>Display names of branches (by id), for statistics and lists.</summary>
    Task<IReadOnlyDictionary<long, string>> GetBranchNamesAsync(IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken = default);

    /// <summary>English display names of doctors (by id), for statistics and lists.</summary>
    Task<IReadOnlyDictionary<long, string>> GetDoctorNamesAsync(IReadOnlyCollection<long> doctorIds, CancellationToken cancellationToken = default);

    /// <summary>The doctor linked to this staff membership (null when the member isn't a doctor).</summary>
    Task<long?> GetDoctorIdForMembershipAsync(long membershipId, CancellationToken cancellationToken = default);

    /// <summary>The doctor's time off overlapping [fromUtc, toUtc).</summary>
    Task<IReadOnlyList<TimeRange>> GetDoctorTimeOffAsync(long doctorId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
