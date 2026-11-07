using Microsoft.EntityFrameworkCore;
using Shifa.Patients.Contracts;
using Shifa.Patients.Data;

namespace Shifa.Patients.Repositories;

/// <summary>The public face of the Patients module (Patients.Contracts). Runs in the caller's tenant context.</summary>
public sealed class PatientsApi : IPatientsApi
{
    private readonly PatientsDbContext _context;

    public PatientsApi(PatientsDbContext context)
    {
        _context = context;
    }

    public Task<bool> PatientExistsAsync(long patientId, CancellationToken cancellationToken = default) =>
        _context.Patients.AnyAsync(p => p.Id == patientId, cancellationToken);

    public Task<bool> HasPortalAccessAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default) =>
        _context.PatientAccountAccesses.AnyAsync(a => a.PhoneNumber == normalizedPhoneNumber, cancellationToken);

    public async Task<IReadOnlyList<PortalPatientAccess>> GetPortalPatientsAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default)
    {
        var rows = await _context.PatientAccountAccesses.AsNoTracking()
            .Where(a => a.PhoneNumber == normalizedPhoneNumber)
            .Select(a => new { a.PatientId, a.Relationship, a.CanBook })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new PortalPatientAccess(r.PatientId, r.Relationship.ToString(), r.CanBook)).ToList();
    }
}
