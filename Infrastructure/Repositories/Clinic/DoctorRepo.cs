using System.Net;
using AutoMapper;
using Core;
using Core.Common;
using Core.DTO.Clinic;
using Core.DTO.Tenancy;
using Core.Entities.Clinic;
using Core.Enums.Common;
using Core.Interfaces.Clinic;
using Core.Interfaces.Clinical;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Core.Interfaces.Tenancy;
using Core.Models.Clinic;
using Core.Pagination;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Clinic;

/// <summary>
/// Doctors, their branches, weekly schedules and time off. Branch scope rules:
/// <list type="bullet">
/// <item>a member limited to some branches only sees doctors who work in one of them, and only those branches, schedules and prices;</item>
/// <item>they can add or remove a doctor's branches only inside their own scope; branches outside it are never touched;</item>
/// <item>actions that affect the doctor everywhere (deactivate, time off) need the doctor to work only inside their scope;</item>
/// <item>a schedule can be set only at a branch in scope.</item>
/// </list>
/// The plan's MaxDoctors limit applies.
/// </summary>
public class DoctorRepo : IDoctorRepo
{
    private readonly ClinicDbContext _context;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenantContext;
    private readonly IAccessContext _access;
    private readonly ITenancyApi _tenancy;
    private readonly IIdentityApi _identity;
    private readonly IClinicalApi _clinical;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public DoctorRepo(ClinicDbContext context, IMapper mapper, ITenantContext tenantContext, IAccessContext access, ITenancyApi tenancy,
        IIdentityApi identity, IClinicalApi clinical, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _clinical = clinical;
        _context = context;
        _mapper = mapper;
        _tenantContext = tenantContext;
        _access = access;
        _tenancy = tenancy;
        _identity = identity;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchDoctorsModel model, CancellationToken cancellationToken)
    {
        var query = Visible(_context.Doctors.AsNoTracking());
        if (model.SpecialtyId is not null)
            query = query.Where(d => _context.DoctorSpecialties.Any(s => s.DoctorId == d.Id && s.SpecialtyId == model.SpecialtyId));
        if (model.BranchId is long branchId)
        {
            // Asking for a branch outside the scope simply finds nothing.
            query = InScopeBranch(branchId)
                ? query.Where(d => _context.DoctorBranches.Any(b => b.DoctorId == d.Id && b.BranchId == branchId))
                : query.Where(_ => false);
        }
        if (!string.IsNullOrWhiteSpace(model.Text))
        {
            var text = model.Text.Trim();
            query = query.Where(d => d.FullNameEn.Contains(text) || d.FullNameAr.Contains(text));
        }

        var page = await query.OrderBy(d => d.FullNameEn).ToPagedListAsync(model, cancellationToken);
        var dtos = await ToDtosAsync(page.Items, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK,
            data: new PagedList<DoctorDto>(dtos, page.TotalCount, page.PageNumber, page.PageSize));
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var doctor = await Visible(_context.Doctors.AsNoTracking()).FirstOrDefaultAsync(d => d.Id == id, cancellationToken) ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: (await ToDtosAsync([doctor], cancellationToken))[0]);
    }

    public async Task<Response> Add(AddDoctorModel model, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("No tenant context.");

        var limit = await _tenancy.GetEntitlementAsync(tenantId, EntitlementCodes.MaxDoctors, cancellationToken);
        if (!limit.Enabled ||
            (limit.Limit is int max && await _context.Doctors.CountAsync(d => d.Status == StatusEnum.Active, cancellationToken) >= max))
            throw new BusinessException(_localizer["DoctorLimitReached"].Value, HttpStatusCode.BadRequest);

        var specialtyIds = model.SpecialtyIds.Distinct().ToList();
        var branchIds = model.BranchIds.Distinct().ToList();
        if (!_access.HasAllBranches && branchIds.Count == 0)
            throw new BusinessException(_localizer["Required"].Value, HttpStatusCode.BadRequest);

        await ValidateSpecialtiesAsync(specialtyIds, cancellationToken);
        await ValidateBranchesAsync(branchIds, cancellationToken);

        var doctor = _mapper.Map<Doctor>(model);
        _context.Doctors.Add(doctor);
        foreach (var specialtyId in specialtyIds)
            _context.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = doctor.Id, SpecialtyId = specialtyId });
        foreach (var branchId in branchIds)
            _context.DoctorBranches.Add(new DoctorBranch { DoctorId = doctor.Id, BranchId = branchId });

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Doctors.Created", "Doctor", doctor.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: doctor.Id);
    }

    public async Task<Response> Update(UpdateDoctorModel model, CancellationToken cancellationToken)
    {
        var doctor = await Visible(_context.Doctors).FirstOrDefaultAsync(d => d.Id == model.Id, cancellationToken) ?? throw NotFound();

        var specialtyIds = model.SpecialtyIds.Distinct().ToList();
        var requestedBranchIds = model.BranchIds.Distinct().ToList();
        await ValidateSpecialtiesAsync(specialtyIds, cancellationToken);
        await ValidateBranchesAsync(requestedBranchIds, cancellationToken);

        _mapper.Map(model, doctor);

        var currentSpecialties = await _context.DoctorSpecialties.Where(s => s.DoctorId == doctor.Id).ToListAsync(cancellationToken);
        _context.DoctorSpecialties.RemoveRange(currentSpecialties.Where(s => !specialtyIds.Contains(s.SpecialtyId)));
        foreach (var specialtyId in specialtyIds.Where(id => currentSpecialties.All(s => s.SpecialtyId != id)))
            _context.DoctorSpecialties.Add(new DoctorSpecialty { DoctorId = doctor.Id, SpecialtyId = specialtyId });

        // Only branches inside the caller's scope can be added or removed; others stay as they are.
        var currentBranches = await _context.DoctorBranches.Where(b => b.DoctorId == doctor.Id).ToListAsync(cancellationToken);
        var removed = currentBranches.Where(b => _access.CanAccessBranch(b.BranchId) && !requestedBranchIds.Contains(b.BranchId)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(b => b.Id).ToList();
            _context.DoctorSchedules.RemoveRange(await _context.DoctorSchedules.Where(s => removedIds.Contains(s.DoctorBranchId)).ToListAsync(cancellationToken));
            _context.DoctorBranches.RemoveRange(removed);
        }
        foreach (var branchId in requestedBranchIds.Where(id => currentBranches.All(b => b.BranchId != id)))
            _context.DoctorBranches.Add(new DoctorBranch { DoctorId = doctor.Id, BranchId = branchId });

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Doctors.Updated", "Doctor", doctor.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> ChangeStatus(long id, StatusEnum status, CancellationToken cancellationToken)
    {
        var doctor = await Visible(_context.Doctors).FirstOrDefaultAsync(d => d.Id == id, cancellationToken) ?? throw NotFound();
        await EnsureOnlyInScopeAsync(doctor.Id, cancellationToken);

        // An inactive doctor can't write visits, so open visits would stay open for ever (ADR-010).
        if (status != StatusEnum.Active && doctor.Status == StatusEnum.Active)
            await EnsureNoOpenVisitsAsync(doctor.Id, cancellationToken);

        doctor.Status = status;
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync($"Doctors.{status}", "Doctor", id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> SetSchedule(SetDoctorScheduleModel model, CancellationToken cancellationToken)
    {
        if (!InScopeBranch(model.BranchId) ||
            !await _context.Branches.AnyAsync(b => b.Id == model.BranchId && b.Status == StatusEnum.Active, cancellationToken))
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.NotFound);

        var doctorBranch = await _context.DoctorBranches
            .FirstOrDefaultAsync(b => b.DoctorId == model.DoctorId && b.BranchId == model.BranchId, cancellationToken)
            ?? throw new BusinessException(_localizer["DoctorNotAtBranch"].Value, HttpStatusCode.BadRequest);

        // Within the request: no two windows of the same day overlap.
        foreach (var day in model.Items.GroupBy(i => i.DayOfWeek))
        {
            var ordered = day.OrderBy(i => i.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                    throw new BusinessException(_localizer["ScheduleOverlap"].Value, HttpStatusCode.BadRequest);
        }

        // Against the doctor's hours at other branches: a doctor can't be in two places at once.
        var otherSchedules = await (
            from s in _context.DoctorSchedules.AsNoTracking()
            join b in _context.DoctorBranches on s.DoctorBranchId equals b.Id
            where b.DoctorId == model.DoctorId && b.BranchId != model.BranchId
            select s).ToListAsync(cancellationToken);
        if (model.Items.Any(i => otherSchedules.Any(o => o.DayOfWeek == i.DayOfWeek && i.StartTime < o.EndTime && o.StartTime < i.EndTime)))
            throw new BusinessException(_localizer["ScheduleOverlap"].Value, HttpStatusCode.BadRequest);

        var current = await _context.DoctorSchedules.Where(s => s.DoctorBranchId == doctorBranch.Id).ToListAsync(cancellationToken);
        _context.DoctorSchedules.RemoveRange(current);
        foreach (var item in model.Items)
        {
            var schedule = _mapper.Map<DoctorSchedule>(item);
            schedule.DoctorBranchId = doctorBranch.Id;
            _context.DoctorSchedules.Add(schedule);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Doctors.ScheduleChanged", "Doctor", model.DoctorId, detail: $"branch {model.BranchId}", cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> AddTimeOff(AddTimeOffModel model, CancellationToken cancellationToken)
    {
        var doctor = await Visible(_context.Doctors.AsNoTracking()).FirstOrDefaultAsync(d => d.Id == model.DoctorId, cancellationToken) ?? throw NotFound();
        await EnsureOnlyInScopeAsync(doctor.Id, cancellationToken);

        var timeOff = _mapper.Map<DoctorTimeOff>(model);
        timeOff.DoctorId = doctor.Id;
        _context.DoctorTimeOffs.Add(timeOff);
        await _context.SaveChangesAsync(cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: timeOff.Id);
    }

    public async Task<Response> DeleteTimeOff(long id, CancellationToken cancellationToken)
    {
        var timeOff = await _context.DoctorTimeOffs.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new BusinessException(_localizer["TimeOffNotFound"].Value, HttpStatusCode.NotFound);

        if (!await Visible(_context.Doctors).AnyAsync(d => d.Id == timeOff.DoctorId, cancellationToken))
            throw new BusinessException(_localizer["TimeOffNotFound"].Value, HttpStatusCode.NotFound);
        await EnsureOnlyInScopeAsync(timeOff.DoctorId, cancellationToken);

        _context.DoctorTimeOffs.Remove(timeOff);
        await _context.SaveChangesAsync(cancellationToken);

        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    /// <summary>All branches (and doctors with at least one branch in scope) for a member limited to some branches.</summary>
    private IQueryable<Doctor> Visible(IQueryable<Doctor> doctors)
    {
        if (_access.HasAllBranches)
            return doctors;

        var scope = _access.BranchIds.ToList();
        return doctors.Where(d => _context.DoctorBranches.Any(b => b.DoctorId == d.Id && scope.Contains(b.BranchId)));
    }

    private bool InScopeBranch(long branchId) => _access.HasAllBranches || _access.BranchIds.Contains(branchId);

    /// <summary>Actions that affect the doctor at every branch need the doctor to work only inside the caller's scope.</summary>
    private async Task EnsureOnlyInScopeAsync(long doctorId, CancellationToken cancellationToken)
    {
        if (_access.HasAllBranches)
            return;

        var scope = _access.BranchIds.ToList();
        if (await _context.DoctorBranches.AnyAsync(b => b.DoctorId == doctorId && !scope.Contains(b.BranchId), cancellationToken))
            throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);
    }

    private async Task ValidateSpecialtiesAsync(List<long> specialtyIds, CancellationToken cancellationToken)
    {
        var existing = await _context.Specialties.CountAsync(s => specialtyIds.Contains(s.Id) && s.Status == StatusEnum.Active, cancellationToken);
        if (existing != specialtyIds.Count)
            throw new BusinessException(_localizer["SpecialtyNotFound"].Value, HttpStatusCode.BadRequest);
    }

    /// <summary>Branches must exist, be active and be inside the caller's scope (others look like "not found").</summary>
    private async Task ValidateBranchesAsync(List<long> branchIds, CancellationToken cancellationToken)
    {
        var scope = _access.BranchIds.ToList();
        var allBranches = _access.HasAllBranches;
        var existing = await _context.Branches.CountAsync(
            b => branchIds.Contains(b.Id) && b.Status == StatusEnum.Active && (allBranches || scope.Contains(b.Id)), cancellationToken);
        if (existing != branchIds.Count)
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.BadRequest);
    }

    /// <summary>Loads specialties, branches, schedules and time off for a page of doctors, limited to the caller's branches.</summary>
    private async Task<List<DoctorDto>> ToDtosAsync(List<Doctor> doctors, CancellationToken cancellationToken)
    {
        var ids = doctors.Select(d => d.Id).ToList();
        var allBranches = _access.HasAllBranches;
        var scope = _access.BranchIds.ToList();

        var specialties = await _context.DoctorSpecialties.AsNoTracking().Where(s => ids.Contains(s.DoctorId)).ToListAsync(cancellationToken);
        var branches = await _context.DoctorBranches.AsNoTracking()
            .Where(b => ids.Contains(b.DoctorId) && (allBranches || scope.Contains(b.BranchId))).ToListAsync(cancellationToken);
        var branchRowIds = branches.Select(b => b.Id).ToList();
        var schedules = await _context.DoctorSchedules.AsNoTracking().Where(s => branchRowIds.Contains(s.DoctorBranchId)).ToListAsync(cancellationToken);
        var timeOffs = await _context.DoctorTimeOffs.AsNoTracking().Where(t => ids.Contains(t.DoctorId)).OrderBy(t => t.FromAt).ToListAsync(cancellationToken);

        return doctors.Select(d =>
        {
            var doctorBranches = branches.Where(b => b.DoctorId == d.Id).ToList();
            var dto = _mapper.Map<DoctorDto>(d);
            dto.SpecialtyIds = specialties.Where(s => s.DoctorId == d.Id).Select(s => s.SpecialtyId).OrderBy(x => x).ToList();
            dto.BranchIds = doctorBranches.Select(b => b.BranchId).OrderBy(x => x).ToList();
            dto.Schedules = schedules
                .Where(s => doctorBranches.Any(b => b.Id == s.DoctorBranchId))
                .OrderBy(s => s.DayOfWeek).ThenBy(s => s.StartTime)
                .Select(s =>
                {
                    var schedule = _mapper.Map<DoctorScheduleDto>(s);
                    schedule.BranchId = doctorBranches.First(b => b.Id == s.DoctorBranchId).BranchId;
                    return schedule;
                }).ToList();
            dto.TimeOffs = _mapper.Map<List<DoctorTimeOffDto>>(timeOffs.Where(t => t.DoctorId == d.Id));
            return dto;
        }).ToList();
    }

    public async Task<Response> LinkAccount(LinkDoctorAccountModel model, CancellationToken cancellationToken)
    {
        var doctor = await Visible(_context.Doctors).FirstOrDefaultAsync(d => d.Id == model.DoctorId, cancellationToken) ?? throw NotFound();
        await EnsureOnlyInScopeAsync(doctor.Id, cancellationToken);

        if (model.MembershipId is long membershipId)
        {
            if (!await _identity.IsActiveMembershipAsync(membershipId, cancellationToken))
                throw new BusinessException(_localizer["StaffNotFound"].Value, HttpStatusCode.BadRequest);
            if (await _context.Doctors.AnyAsync(d => d.Id != doctor.Id && d.MembershipId == membershipId, cancellationToken))
                throw new BusinessException(_localizer["DoctorAccountInUse"].Value, HttpStatusCode.BadRequest);
        }

        // Changing the account while visits are open would hand those visits to another person (or orphan them).
        if (doctor.MembershipId != model.MembershipId && doctor.MembershipId is not null)
            await EnsureNoOpenVisitsAsync(doctor.Id, cancellationToken);

        doctor.MembershipId = model.MembershipId;
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex is not DbUpdateConcurrencyException
                                           && ex.InnerException?.Message.Contains("IX_Doctors_TenantId_MembershipId", StringComparison.Ordinal) == true)
        {
            throw new BusinessException(_localizer["DoctorAccountInUse"].Value, HttpStatusCode.BadRequest);
        }

        await _audit.LogAsync("Doctors.AccountLinked", "Doctor", doctor.Id, detail: model.MembershipId?.ToString() ?? "unlinked",
            cancellationToken: cancellationToken);
        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    private async Task EnsureNoOpenVisitsAsync(long doctorId, CancellationToken cancellationToken)
    {
        if (await _clinical.HasOpenVisitsAsync(doctorId, cancellationToken))
            throw new BusinessException(_localizer["DoctorHasOpenVisits"].Value, HttpStatusCode.BadRequest);
    }

    private BusinessException NotFound() => new(_localizer["DoctorNotFound"].Value, HttpStatusCode.NotFound);
}
