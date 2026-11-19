using System.Linq.Expressions;
using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Patients;
using Core.Entities.Patients;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Models.Patients;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Patients;

/// <summary>
/// Patient profiles for staff. Patients belong to the tenant (not to a branch), so access is controlled by permissions
/// (Patients.Read / Create / Update) and the Tenant filter. Opening a full profile is audited.
/// </summary>
public class PatientRepo : IPatientRepo
{
    private const int MaxNumberAttempts = 5;

    private readonly PatientsDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;
    private readonly IAuditService _audit;
    private readonly TimeProvider _time;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PatientRepo(PatientsDbContext context, IMapper mapper, ITenantContext tenant, IAuditService audit, TimeProvider time, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _tenant = tenant;
        _audit = audit;
        _time = time;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchPatientsModel model, CancellationToken cancellationToken)
    {
        var query = SortPatients(_context.Patients.AsNoTracking().Where(FilterPatients(model)), model);

        var paged = await query
            .ProjectTo<SearchPatientsDto>(_mapper.ConfigurationProvider)
            .ToPagedListAsync(model, cancellationToken);

        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken) ?? throw NotFound();

        var identifiers = await _context.PatientIdentifiers.AsNoTracking().Where(i => i.PatientId == id).ToListAsync(cancellationToken);
        var guardians = await _context.PatientGuardians.AsNoTracking().Where(g => g.PatientId == id).OrderBy(g => g.Id).ToListAsync(cancellationToken);
        var consents = await _context.PatientConsents.AsNoTracking().Where(c => c.PatientId == id).OrderBy(c => c.GrantedAt).ToListAsync(cancellationToken);
        var access = await _context.PatientAccountAccesses.AsNoTracking().Where(a => a.PatientId == id).OrderBy(a => a.Id).ToListAsync(cancellationToken);

        await _audit.LogAsync("Patients.Viewed", "Patient", id, cancellationToken: cancellationToken);

        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: ToDto(patient, identifiers, guardians, consents, access));
    }

    public async Task<Response> Add(AddPatientModel model, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        if (model.DateOfBirth > today)
            throw new BusinessException(_localizer["InvalidDateOfBirth"].Value, HttpStatusCode.BadRequest);

        var identifiers = NormalizeIdentifiers(model.Identifiers);
        await EnsureIdentifiersFreeAsync(identifiers, ignorePatientId: null, cancellationToken);

        // Registrations of one tenant take their number one at a time; without the lock a burst of concurrent
        // registrations could use up the retries below and fail.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.Database.AcquireAsync($"patients:number:{_tenant.TenantId}", cancellationToken);

        long patientId = 0;
        for (var attempt = 1; ; attempt++)
        {
            // Next running number of this tenant. Soft-deleted rows still count, so a number is never reused;
            // only the soft-delete filter is lifted, the Tenant filter stays on.
            var next = (await _context.Patients.IgnoreQueryFilters([ModuleDbContext.SoftDeleteFilter])
                .MaxAsync(p => (int?)p.Number, cancellationToken) ?? 0) + 1;

            var patient = _mapper.Map<Patient>(model);
            patient.Number = next;
            patient.Mrn = Patient.FormatMrn(next);

            if (patient.IsMinor(today) && model.Guardians.Count == 0)
                throw new BusinessException(_localizer["GuardianRequiredForMinor"].Value, HttpStatusCode.BadRequest);

            _context.Patients.Add(patient);
            foreach (var identifier in identifiers)
                _context.PatientIdentifiers.Add(new PatientIdentifier { PatientId = patient.Id, Type = identifier.Type, Value = identifier.Value });
            foreach (var guardian in model.Guardians)
            {
                var row = _mapper.Map<PatientGuardian>(guardian);
                row.PatientId = patient.Id;
                _context.PatientGuardians.Add(row);
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                patientId = patient.Id;
                break;
            }
            catch (DbUpdateException ex) when (attempt < MaxNumberAttempts && IsNumberConflict(ex))
            {
                // Another request took the same number at the same moment: the unique index decided, try the next one.
                _context.ChangeTracker.Clear();
            }
            catch (DbUpdateException ex) when (IsIdentifierConflict(ex))
            {
                throw new BusinessException(_localizer["PatientAlreadyExists"].Value, HttpStatusCode.BadRequest);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        await _audit.LogAsync("Patients.Created", "Patient", patientId, cancellationToken: cancellationToken);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: patientId);
    }

    public async Task<Response> Update(UpdatePatientModel model, CancellationToken cancellationToken)
    {
        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken) ?? throw NotFound();

        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        if (model.DateOfBirth > today)
            throw new BusinessException(_localizer["InvalidDateOfBirth"].Value, HttpStatusCode.BadRequest);

        var identifiers = NormalizeIdentifiers(model.Identifiers);
        await EnsureIdentifiersFreeAsync(identifiers, patient.Id, cancellationToken);

        _mapper.Map(model, patient);

        var current = await _context.PatientIdentifiers.Where(i => i.PatientId == patient.Id).ToListAsync(cancellationToken);
        _context.PatientIdentifiers.RemoveRange(current.Where(c => identifiers.All(i => i.Type != c.Type || i.Value != c.Value)));
        foreach (var identifier in identifiers.Where(i => current.All(c => c.Type != i.Type || c.Value != i.Value)))
            _context.PatientIdentifiers.Add(new PatientIdentifier { PatientId = patient.Id, Type = identifier.Type, Value = identifier.Value });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsIdentifierConflict(ex))
        {
            throw new BusinessException(_localizer["PatientAlreadyExists"].Value, HttpStatusCode.BadRequest);
        }

        await _audit.LogAsync("Patients.Updated", "Patient", patient.Id, cancellationToken: cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> AddGuardian(AddGuardianModel model, CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(model.PatientId, cancellationToken);

        var guardian = _mapper.Map<PatientGuardian>(model);
        _context.PatientGuardians.Add(guardian);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Patients.GuardianAdded", "Patient", model.PatientId, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: guardian.Id);
    }

    public async Task<Response> DeleteGuardian(long id, CancellationToken cancellationToken)
    {
        var guardian = await _context.PatientGuardians.FirstOrDefaultAsync(g => g.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["GuardianNotFound"].Value, HttpStatusCode.NotFound);

        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        var patient = await _context.Patients.AsNoTracking().FirstAsync(p => p.Id == guardian.PatientId, cancellationToken);
        var remaining = await _context.PatientGuardians.CountAsync(g => g.PatientId == guardian.PatientId && g.Id != id, cancellationToken);
        if (patient.IsMinor(today) && remaining == 0)
            throw new BusinessException(_localizer["GuardianRequiredForMinor"].Value, HttpStatusCode.BadRequest);

        _context.PatientGuardians.Remove(guardian);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Patients.GuardianRemoved", "Patient", guardian.PatientId, cancellationToken: cancellationToken);

        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> GrantConsent(GrantConsentModel model, CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(model.PatientId, cancellationToken);

        // One active consent of each type: granting again while it is active is a no-op.
        var existing = await _context.PatientConsents
            .FirstOrDefaultAsync(c => c.PatientId == model.PatientId && c.ConsentType == model.ConsentType && c.RevokedAt == null, cancellationToken);
        if (existing is not null)
            return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: existing.Id);

        var consent = _mapper.Map<PatientConsent>(model);
        consent.GrantedAt = _time.GetUtcNow();
        _context.PatientConsents.Add(consent);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Patients.ConsentGranted", "Patient", model.PatientId, detail: model.ConsentType.ToString(), cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: consent.Id);
    }

    public async Task<Response> RevokeConsent(long id, CancellationToken cancellationToken)
    {
        var consent = await _context.PatientConsents.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["ConsentNotFound"].Value, HttpStatusCode.NotFound);

        if (consent.RevokedAt is null)
        {
            consent.RevokedAt = _time.GetUtcNow();
            await _context.SaveChangesAsync(cancellationToken);
            await _audit.LogAsync("Patients.ConsentRevoked", "Patient", consent.PatientId, detail: consent.ConsentType.ToString(), cancellationToken: cancellationToken);
        }

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> LinkPortalAccess(LinkPortalAccessModel model, CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(model.PatientId, cancellationToken);
        var phone = PhoneNumbers.Normalize(model.PhoneNumber)!;

        var existing = await _context.PatientAccountAccesses
            .FirstOrDefaultAsync(a => a.PatientId == model.PatientId && a.PhoneNumber == phone, cancellationToken);
        if (existing is not null)
        {
            existing.Relationship = model.Relationship;
            existing.CanBook = model.CanBook;
            await _context.SaveChangesAsync(cancellationToken);
            return new Response(_localizer["Updated"].Value, HttpStatusCode.OK, data: existing.Id);
        }

        var access = _mapper.Map<PatientAccountAccess>(model);
        access.PhoneNumber = phone;
        _context.PatientAccountAccesses.Add(access);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Patients.PortalAccessLinked", "Patient", model.PatientId, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: access.Id);
    }

    public async Task<Response> RemovePortalAccess(long id, CancellationToken cancellationToken)
    {
        var access = await _context.PatientAccountAccesses.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["PortalAccessNotFound"].Value, HttpStatusCode.NotFound);

        _context.PatientAccountAccesses.Remove(access);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Patients.PortalAccessRemoved", "Patient", access.PatientId, cancellationToken: cancellationToken);

        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    private async Task EnsurePatientAsync(long patientId, CancellationToken cancellationToken)
    {
        if (!await _context.Patients.AnyAsync(p => p.Id == patientId, cancellationToken))
            throw NotFound();
    }

    /// <summary>Trims values, drops exact duplicates inside the request.</summary>
    private static List<PatientIdentifierModel> NormalizeIdentifiers(IEnumerable<PatientIdentifierModel> identifiers) =>
        identifiers
            .Select(i => new PatientIdentifierModel { Type = i.Type, Value = i.Value.Trim().ToUpperInvariant() })
            .DistinctBy(i => (i.Type, i.Value))
            .ToList();

    private async Task EnsureIdentifiersFreeAsync(List<PatientIdentifierModel> identifiers, long? ignorePatientId, CancellationToken cancellationToken)
    {
        foreach (var identifier in identifiers)
        {
            var type = identifier.Type;
            var value = identifier.Value;
            if (await _context.PatientIdentifiers.AnyAsync(
                    i => i.Type == type && i.Value == value && i.PatientId != ignorePatientId, cancellationToken))
                throw new BusinessException(_localizer["PatientAlreadyExists"].Value, HttpStatusCode.BadRequest);
        }
    }

    private static bool IsNumberConflict(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_Patients_TenantId_Number", StringComparison.Ordinal) == true
        || ex.InnerException?.Message.Contains("IX_Patients_TenantId_Mrn", StringComparison.Ordinal) == true;

    private static bool IsIdentifierConflict(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_PatientIdentifiers_TenantId_Type_Value", StringComparison.Ordinal) == true;

    private PatientDto ToDto(Patient patient, List<PatientIdentifier> identifiers, List<PatientGuardian> guardians,
        List<PatientConsent> consents, List<PatientAccountAccess> access)
    {
        var dto = _mapper.Map<PatientDto>(patient);
        dto.Identifiers = _mapper.Map<List<PatientIdentifierDto>>(identifiers);
        dto.Guardians = _mapper.Map<List<PatientGuardianDto>>(guardians);
        dto.Consents = _mapper.Map<List<PatientConsentDto>>(consents);
        dto.PortalAccess = _mapper.Map<List<PatientPortalAccessDto>>(access);
        return dto;
    }

    /// <summary>Search text matches name, MRN, the normalized phone or an exact identifier (national ID / passport).</summary>
    private Expression<Func<Patient, bool>> FilterPatients(SearchPatientsModel model)
    {
        var text = model.SearchText;
        var phone = text is null ? null : PhoneNumbers.Normalize(text);
        return p => (text == null
                     || p.FullName.Contains(text)
                     || p.Mrn.Contains(text)
                     || (phone != null && p.PhoneNumber == phone)
                     || _context.PatientIdentifiers.Any(i => i.PatientId == p.Id && i.Value == text))
                    && (model.Gender == null || p.Gender == model.Gender);
    }

    private static IQueryable<Patient> SortPatients(IQueryable<Patient> query, SearchPatientsModel model) => model.SortBy switch
    {
        "fullName" => query.OrderBy(model.SortType, p => p.FullName).ThenBy(p => p.Id),
        "mrn" => query.OrderBy(model.SortType, p => p.Number).ThenBy(p => p.Id),
        "dateOfBirth" => query.OrderBy(model.SortType, p => p.DateOfBirth).ThenBy(p => p.Id),
        _ => query.OrderBy(model.SortType, p => p.CreationDate).ThenBy(p => p.Id),
    };

    private BusinessException NotFound() => new(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);
}
