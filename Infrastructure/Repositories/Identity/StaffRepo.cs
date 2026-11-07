using System.Net;
using Core;
using Core.Common;
using Core.DTO.Identity;
using Core.DTO.Tenancy;
using Core.Entities.Identity;
using Core.Enums.Identity;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Core.Interfaces.Tenancy;
using Core.Models.Identity;
using Infrastructure.Data;
using Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Identity;

/// <summary>
/// Staff (memberships) of the current tenant. Power rules:
/// <list type="bullet">
/// <item>you can only grant roles whose permissions you hold, and branches inside your own scope;</item>
/// <item>you can only change or suspend someone whose current permissions and branches you also hold
/// (a staff manager can't demote or lock out a TenantAdmin);</item>
/// <item>the last active TenantAdmin can't be suspended or lose the TenantAdmin role.</item>
/// </list>
/// </summary>
public class StaffRepo : IStaffRepo
{
    private readonly IdentityDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IAccessContext _access;
    private readonly IPasswordHasher<User> _hasher;
    private readonly ITenancyApi _tenancy;
    private readonly IClinicApi _clinic;
    private readonly MembershipAccessCache _cache;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public StaffRepo(IdentityDbContext context, ITenantContext tenantContext, IAccessContext access,
        IPasswordHasher<User> hasher, ITenancyApi tenancy, IClinicApi clinic, MembershipAccessCache cache,
        IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _tenantContext = tenantContext;
        _access = access;
        _hasher = hasher;
        _tenancy = tenancy;
        _clinic = clinic;
        _cache = cache;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> Search(SearchStaffModel model, CancellationToken cancellationToken)
    {
        var paged = await ProjectStaff(_context.Memberships.AsNoTracking()).OrderBy(s => s.FullName).ToPagedListAsync(model, cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: paged);
    }

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        var staff = await ProjectStaff(_context.Memberships.AsNoTracking().Where(m => m.Id == id)).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: staff);
    }

    public async Task<Response> Add(AddStaffModel model, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? throw new InvalidOperationException("No tenant context.");
        await EnsureEmployeeCapacityAsync(tenantId, cancellationToken);

        var roleIds = model.RoleIds.Distinct().ToList();
        var branchIds = model.AllBranches ? [] : model.BranchIds.Distinct().ToList();
        await ValidateAssignmentAsync(roleIds, model.AllBranches, branchIds, cancellationToken);

        var email = model.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            if (string.IsNullOrEmpty(model.Password))
                throw new BusinessException(_localizer["Required"].Value, HttpStatusCode.BadRequest);

            user = new User { FullName = model.FullName.Trim(), Email = email };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);
            _context.Users.Add(user);
        }
        else if (await _context.Memberships.AnyAsync(m => m.UserId == user.Id, cancellationToken))
        {
            throw new BusinessException(_localizer["StaffAlreadyExists"].Value, HttpStatusCode.BadRequest);
        }

        var membership = new Membership { UserId = user.Id, AllBranches = model.AllBranches };
        _context.Memberships.Add(membership);
        AddAssignments(membership.Id, roleIds, branchIds);

        await _context.SaveChangesAsync(cancellationToken);
        _cache.InvalidateTenant(tenantId);
        await _audit.LogAsync("Staff.Added", "Membership", membership.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: membership.Id);
    }

    public async Task<Response> Update(UpdateStaffModel model, CancellationToken cancellationToken)
    {
        var membership = await _context.Memberships.FirstOrDefaultAsync(m => m.Id == model.Id, cancellationToken) ?? throw NotFound();
        await EnsureCanManageAsync(membership, cancellationToken);

        var roleIds = model.RoleIds.Distinct().ToList();
        var branchIds = model.AllBranches ? [] : model.BranchIds.Distinct().ToList();
        await ValidateAssignmentAsync(roleIds, model.AllBranches, branchIds, cancellationToken);

        var (adminRoleId, isLastAdmin) = await GetLastTenantAdminStateAsync(membership.Id, cancellationToken);
        if (isLastAdmin && !roleIds.Contains(adminRoleId))
            throw new BusinessException(_localizer["LastTenantAdmin"].Value, HttpStatusCode.BadRequest);

        membership.AllBranches = model.AllBranches;

        var currentRoles = await _context.MembershipRoles.Where(r => r.MembershipId == membership.Id).ToListAsync(cancellationToken);
        _context.MembershipRoles.RemoveRange(currentRoles.Where(r => !roleIds.Contains(r.RoleId)));
        var newRoleIds = roleIds.Where(id => currentRoles.All(r => r.RoleId != id)).ToList();

        var currentBranches = await _context.MembershipBranches.Where(b => b.MembershipId == membership.Id).ToListAsync(cancellationToken);
        _context.MembershipBranches.RemoveRange(currentBranches.Where(b => !branchIds.Contains(b.BranchId)));
        var newBranchIds = branchIds.Where(id => currentBranches.All(b => b.BranchId != id)).ToList();

        AddAssignments(membership.Id, newRoleIds, newBranchIds);

        await _context.SaveChangesAsync(cancellationToken);
        _cache.InvalidateTenant(membership.TenantId);
        await _audit.LogAsync("Staff.Updated", "Membership", membership.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> Suspend(long id, CancellationToken cancellationToken)
    {
        if (id == _access.MembershipId)
            throw new BusinessException(_localizer["CannotSuspendSelf"].Value, HttpStatusCode.BadRequest);

        var membership = await _context.Memberships.FirstOrDefaultAsync(m => m.Id == id, cancellationToken) ?? throw NotFound();
        await EnsureCanManageAsync(membership, cancellationToken);

        var (_, isLastAdmin) = await GetLastTenantAdminStateAsync(membership.Id, cancellationToken);
        if (isLastAdmin)
            throw new BusinessException(_localizer["LastTenantAdmin"].Value, HttpStatusCode.BadRequest);

        return await SaveStatusAsync(membership, MembershipStatusEnum.Suspended, "Staff.Suspended", cancellationToken);
    }

    public async Task<Response> Activate(long id, CancellationToken cancellationToken)
    {
        var membership = await _context.Memberships.FirstOrDefaultAsync(m => m.Id == id, cancellationToken) ?? throw NotFound();
        await EnsureCanManageAsync(membership, cancellationToken);

        // Re-activating counts against the plan like adding someone new.
        if (membership.Status != MembershipStatusEnum.Active)
            await EnsureEmployeeCapacityAsync(membership.TenantId, cancellationToken);

        return await SaveStatusAsync(membership, MembershipStatusEnum.Active, "Staff.Activated", cancellationToken);
    }

    private async Task<Response> SaveStatusAsync(Membership membership, MembershipStatusEnum status, string auditAction, CancellationToken cancellationToken)
    {
        membership.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        _cache.InvalidateTenant(membership.TenantId);
        await _audit.LogAsync(auditAction, "Membership", membership.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    /// <summary>
    /// The plan's MaxEmployees limit. A limit entitlement that is present but disabled means "none allowed".
    /// Concurrent additions can briefly exceed the limit by one; the limit is commercial, not a safety rule.
    /// </summary>
    private async Task EnsureEmployeeCapacityAsync(long tenantId, CancellationToken cancellationToken)
    {
        var limit = await _tenancy.GetEntitlementAsync(tenantId, EntitlementCodes.MaxEmployees, cancellationToken);
        if (!limit.Enabled)
            throw new BusinessException(_localizer["EmployeeLimitReached"].Value, HttpStatusCode.BadRequest);

        if (limit.Limit is int max &&
            await _context.Memberships.CountAsync(m => m.Status == MembershipStatusEnum.Active, cancellationToken) >= max)
            throw new BusinessException(_localizer["EmployeeLimitReached"].Value, HttpStatusCode.BadRequest);
    }

    /// <summary>You can only change someone whose current permissions and branches you hold yourself.</summary>
    private async Task EnsureCanManageAsync(Membership target, CancellationToken cancellationToken)
    {
        var targetPermissions = await (
            from mr in _context.MembershipRoles
            where mr.MembershipId == target.Id
            join p in _context.RolePermissions on mr.RoleId equals p.RoleId
            select p.PermissionCode).Distinct().ToListAsync(cancellationToken);

        if (targetPermissions.Any(code => !_access.HasPermission(code)))
            throw Forbidden();

        if (_access.HasAllBranches)
            return;

        if (target.AllBranches)
            throw Forbidden();

        var targetBranches = await _context.MembershipBranches.Where(b => b.MembershipId == target.Id).Select(b => b.BranchId).ToListAsync(cancellationToken);
        if (targetBranches.Any(id => !_access.CanAccessBranch(id)))
            throw Forbidden();
    }

    /// <summary>Whether <paramref name="membershipId"/> is the only active member holding the TenantAdmin role.</summary>
    private async Task<(long AdminRoleId, bool IsLastAdmin)> GetLastTenantAdminStateAsync(long membershipId, CancellationToken cancellationToken)
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.IsSystemDefault && r.Name == Permissions.TenantAdminRole)
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var activeAdmins = await (
            from mr in _context.MembershipRoles
            where mr.RoleId == adminRoleId
            join m in _context.Memberships on mr.MembershipId equals m.Id
            where m.Status == MembershipStatusEnum.Active
            select m.Id).Distinct().ToListAsync(cancellationToken);

        return (adminRoleId, activeAdmins.Count == 1 && activeAdmins[0] == membershipId);
    }

    /// <summary>
    /// Roles and branches must exist in this tenant, and the caller can only hand out what they hold themselves:
    /// roles whose permissions they have, and branches inside their own branch scope.
    /// </summary>
    private async Task ValidateAssignmentAsync(List<long> roleIds, bool allBranches, List<long> branchIds, CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0 || (!allBranches && branchIds.Count == 0))
            throw new BusinessException(_localizer["Required"].Value, HttpStatusCode.BadRequest);

        var existingRoles = await _context.Roles.CountAsync(r => roleIds.Contains(r.Id), cancellationToken);
        if (existingRoles != roleIds.Count)
            throw new BusinessException(_localizer["RoleNotFound"].Value, HttpStatusCode.BadRequest);

        var rolePermissions = await _context.RolePermissions.AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId)).Select(p => p.PermissionCode).Distinct().ToListAsync(cancellationToken);
        if (rolePermissions.Any(code => !_access.HasPermission(code)))
            throw Forbidden();

        if (allBranches)
        {
            if (!_access.HasAllBranches)
                throw Forbidden();
            return;
        }

        var existingBranches = await _clinic.GetExistingBranchIdsAsync(branchIds, cancellationToken);
        if (existingBranches.Count != branchIds.Count)
            throw new BusinessException(_localizer["BranchNotFound"].Value, HttpStatusCode.BadRequest);

        if (branchIds.Any(id => !_access.CanAccessBranch(id)))
            throw Forbidden();
    }

    private void AddAssignments(long membershipId, IEnumerable<long> roleIds, IEnumerable<long> branchIds)
    {
        foreach (var roleId in roleIds)
            _context.MembershipRoles.Add(new MembershipRole { MembershipId = membershipId, RoleId = roleId });
        foreach (var branchId in branchIds)
            _context.MembershipBranches.Add(new MembershipBranch { MembershipId = membershipId, BranchId = branchId });
    }

    private IQueryable<StaffDto> ProjectStaff(IQueryable<Membership> memberships) =>
        from m in memberships
        join u in _context.Users on m.UserId equals u.Id
        select new StaffDto
        {
            Id = m.Id,
            UserId = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Status = m.Status.ToString(),
            AllBranches = m.AllBranches,
            BranchIds = _context.MembershipBranches.Where(b => b.MembershipId == m.Id).Select(b => b.BranchId).ToList(),
            RoleIds = _context.MembershipRoles.Where(r => r.MembershipId == m.Id).Select(r => r.RoleId).ToList(),
        };

    private BusinessException Forbidden() => new(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);

    private BusinessException NotFound() => new(_localizer["StaffNotFound"].Value, HttpStatusCode.NotFound);
}
