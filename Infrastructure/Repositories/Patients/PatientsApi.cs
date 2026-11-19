using Core.DTO.Patients;
using Core.Enums.Patients;
using Core.Interfaces.Patients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories.Patients;

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

    public Task<PatientBillingContact?> GetBillingContactAsync(long patientId, CancellationToken cancellationToken = default) =>
        _context.Patients.AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new PatientBillingContact(p.FullName, p.Email))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PatientContact?> GetContactAsync(long patientId, CancellationToken cancellationToken = default)
    {
        var patient = await _context.Patients.AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.FullName, p.PhoneNumber, p.Email })
            .FirstOrDefaultAsync(cancellationToken);
        if (patient is null)
            return null;

        var phone = patient.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = await _context.PatientAccountAccesses.AsNoTracking()
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.Relationship == RelationshipEnum.Self ? 0 : 1).ThenBy(a => a.Id)
                .Select(a => a.PhoneNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new PatientContact(patient.FullName, string.IsNullOrWhiteSpace(phone) ? null : phone, patient.Email);
    }

    public Task<bool> HasPortalAccessAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default) =>
        _context.PatientAccountAccesses.AnyAsync(a => a.PhoneNumber == normalizedPhoneNumber, cancellationToken);

    public async Task<IReadOnlyList<DateTimeOffset>> GetRegistrationTimesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc,
        CancellationToken cancellationToken = default) =>
        await _context.Patients.AsNoTracking()
            .Where(p => p.CreationDate >= fromUtc && p.CreationDate < toUtc)
            .Select(p => p.CreationDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PortalPatientAccess>> GetPortalPatientsAsync(string normalizedPhoneNumber, CancellationToken cancellationToken = default)
    {
        var rows = await _context.PatientAccountAccesses.AsNoTracking()
            .Where(a => a.PhoneNumber == normalizedPhoneNumber)
            .Select(a => new { a.PatientId, a.Relationship, a.CanBook })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new PortalPatientAccess(r.PatientId, r.Relationship.ToString(), r.CanBook)).ToList();
    }
}
