using System.Net;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Core;
using Core.Common;
using Core.DTO.Clinical;
using Core.Entities.Clinical;
using Core.Interfaces.Clinical;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Models.Clinical;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinical;

/// <summary>
/// Patient-level clinical data. Allergies and chronic conditions are visible to every Clinical.Read holder in the
/// tenant, for patient safety; the visit list in the summary follows the reader's branch scope (ADR-010).
/// </summary>
public class PatientClinicalRepo : IPatientClinicalRepo
{
    private const int RecentVisits = 20;

    private readonly ClinicalDbContext _context;
    private readonly IMapper _mapper;
    private readonly IPatientsApi _patients;
    private readonly IAccessContext _access;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public PatientClinicalRepo(ClinicalDbContext context, IMapper mapper, IPatientsApi patients, IAccessContext access,
        ICurrentUser currentUser, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _mapper = mapper;
        _patients = patients;
        _access = access;
        _currentUser = currentUser;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> Summary(long patientId, CancellationToken cancellationToken)
    {
        if (!await _patients.PatientExistsAsync(patientId, cancellationToken))
            throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);

        var visits = _context.Visits.AsNoTracking().Where(v => v.PatientId == patientId);
        if (!_access.HasAllBranches)
        {
            var scope = _access.BranchIds.ToList();
            visits = visits.Where(v => scope.Contains(v.BranchId));
        }

        var summary = new PatientClinicalSummaryDto
        {
            PatientId = patientId,
            Allergies = await _context.PatientAllergies.AsNoTracking().Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.Severity).ThenBy(a => a.Allergen)
                .ProjectTo<PatientAllergyDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),
            Conditions = await _context.PatientConditions.AsNoTracking().Where(c => c.PatientId == patientId)
                .OrderBy(c => c.Condition)
                .ProjectTo<PatientConditionDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),
            RecentVisits = await visits.OrderByDescending(v => v.OpenedAt).Take(RecentVisits)
                .ProjectTo<VisitListDto>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken),
        };

        await _audit.LogAsync("Clinical.PatientSummaryViewed", "Patient", patientId, cancellationToken: CancellationToken.None);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: summary);
    }

    public async Task<Response> AddAllergy(AddAllergyModel model, CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(model.PatientId, cancellationToken);

        var allergy = _mapper.Map<PatientAllergy>(model);
        allergy.RecordedByUserId = CurrentUserId();
        _context.PatientAllergies.Add(allergy);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Clinical.AllergyAdded", "Patient", model.PatientId, cancellationToken: CancellationToken.None);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: allergy.Id);
    }

    public async Task<Response> DeleteAllergy(long id, CancellationToken cancellationToken)
    {
        var allergy = await _context.PatientAllergies.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["AllergyNotFound"].Value, HttpStatusCode.NotFound);

        _context.PatientAllergies.Remove(allergy);   // soft delete: the row stays for the record
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Clinical.AllergyRemoved", "Patient", allergy.PatientId, detail: $"allergy {id}", cancellationToken: CancellationToken.None);
        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> AddCondition(AddConditionModel model, CancellationToken cancellationToken)
    {
        await EnsurePatientAsync(model.PatientId, cancellationToken);

        var condition = _mapper.Map<PatientCondition>(model);
        condition.RecordedByUserId = CurrentUserId();
        _context.PatientConditions.Add(condition);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Clinical.ConditionAdded", "Patient", model.PatientId, cancellationToken: CancellationToken.None);
        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: condition.Id);
    }

    public async Task<Response> DeleteCondition(long id, CancellationToken cancellationToken)
    {
        var condition = await _context.PatientConditions.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["ConditionNotFound"].Value, HttpStatusCode.NotFound);

        _context.PatientConditions.Remove(condition);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync("Clinical.ConditionRemoved", "Patient", condition.PatientId, detail: $"condition {id}", cancellationToken: CancellationToken.None);
        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    private async Task EnsurePatientAsync(long patientId, CancellationToken cancellationToken)
    {
        if (!await _patients.PatientExistsAsync(patientId, cancellationToken))
            throw new BusinessException(_localizer["PatientNotFound"].Value, HttpStatusCode.NotFound);
    }

    private long CurrentUserId() => _currentUser.GetUserId() ?? throw new InvalidOperationException("No signed-in user.");
}
