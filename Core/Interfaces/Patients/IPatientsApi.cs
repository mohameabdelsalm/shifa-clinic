using Core.DTO.Patients;

namespace Core.Interfaces.Patients;

/// <summary>Runs in the caller's tenant context: it only ever sees the current tenant's patients.</summary>
public interface IPatientsApi
{
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken = default);

    /// <summary>True when the (normalized) phone number is linked to at least one patient as a portal user.</summary>
    Task<bool> HasPortalAccessAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>The patients a portal user may act for (self, children, wards), and whether they may book for each.</summary>
    Task<IReadOnlyList<PortalPatientAccess>> GetPortalPatientsAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>Name and e-mail for a payment gateway's billing data; null when the patient isn't in this tenant.</summary>
    Task<PatientBillingContact?> GetBillingContactAsync(long patientId, CancellationToken cancellationToken = default);

    /// <summary>Registration times of patients created in [fromUtc, toUtc), for statistics (patients belong to the tenant, not a branch).</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetRegistrationTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
}
