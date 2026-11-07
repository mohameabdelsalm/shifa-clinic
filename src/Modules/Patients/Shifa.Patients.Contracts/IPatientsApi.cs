namespace Shifa.Patients.Contracts;

/// <summary>Runs in the caller's tenant context: it only ever sees the current tenant's patients.</summary>
public interface IPatientsApi
{
    Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken = default);

    /// <summary>True when the (normalized) phone number is linked to at least one patient as a portal user.</summary>
    Task<bool> HasPortalAccessAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default);

    /// <summary>The patients a portal user may act for (self, children, wards), and whether they may book for each.</summary>
    Task<IReadOnlyList<PortalPatientAccess>> GetPortalPatientsAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default);
}

public sealed record PortalPatientAccess(long PatientId, string Relationship, bool CanBook);
