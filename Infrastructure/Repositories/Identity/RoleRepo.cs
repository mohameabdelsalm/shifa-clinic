using System.Net;
using Core;
using Core.Common;
using Core.DTO.Identity;
using Core.Entities.Identity;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Core.Models.Identity;
using Infrastructure.Data;
using Infrastructure.Services.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Repositories.Identity;

public class RoleRepo : IRoleRepo
{
    private readonly IdentityDbContext _context;
    private readonly IAccessContext _access;
    private readonly MembershipAccessCache _cache;
    private readonly IAuditService _audit;
    private readonly IStringLocalizer<LanguageResource> _localizer;

    public RoleRepo(IdentityDbContext context, IAccessContext access,
        MembershipAccessCache cache, IAuditService audit, IStringLocalizer<LanguageResource> localizer)
    {
        _context = context;
        _access = access;
        _cache = cache;
        _audit = audit;
        _localizer = localizer;
    }

    public async Task<Response> GetAll(CancellationToken cancellationToken)
    {
        var roles = await ProjectRoles(_context.Roles.AsNoTracking()).OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: roles);
    }

    public Task<Response> GetPermissions() =>
        Task.FromResult(new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: Permissions.All));

    public async Task<Response> View(long id, CancellationToken cancellationToken)
    {
        // Another tenant's role id is simply "not found": the Tenant filter hides it.
        var role = await ProjectRoles(_context.Roles.AsNoTracking().Where(r => r.Id == id)).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFound();
        return new Response(_localizer["Retrieved"].Value, HttpStatusCode.OK, data: role);
    }

    public async Task<Response> Add(AddRoleModel model, CancellationToken cancellationToken)
    {
        var name = model.Name.Trim();
        var permissions = model.Permissions.Distinct().ToList();
        EnsureCanGrant(permissions);

        if (await _context.Roles.AnyAsync(r => r.Name == name, cancellationToken))
            throw new BusinessException(_localizer["RoleNameDuplicated"].Value, HttpStatusCode.BadRequest);

        var role = new Role { Name = name };
        _context.Roles.Add(role);
        foreach (var code in permissions)
            _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = code });

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("Roles.Created", "Role", role.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Created"].Value, HttpStatusCode.OK, data: role.Id);
    }

    public async Task<Response> Update(UpdateRoleModel model, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == model.Id, cancellationToken) ?? throw NotFound();
        var name = model.Name.Trim();

        if (role.IsSystemDefault && (role.Name == Permissions.TenantAdminRole || role.Name != name))
            throw new BusinessException(_localizer["SystemRoleLocked"].Value, HttpStatusCode.BadRequest);

        var permissions = model.Permissions.Distinct().ToList();
        EnsureCanGrant(permissions);

        // Removing a permission is also a change of power: the caller must hold everything the role holds today.
        var current = await _context.RolePermissions.Where(p => p.RoleId == role.Id).ToListAsync(cancellationToken);
        EnsureCanGrant(current.Select(p => p.PermissionCode).ToList());

        if (await _context.Roles.AnyAsync(r => r.Name == name && r.Id != role.Id, cancellationToken))
            throw new BusinessException(_localizer["RoleNameDuplicated"].Value, HttpStatusCode.BadRequest);

        role.Name = name;

        _context.RolePermissions.RemoveRange(current.Where(p => !permissions.Contains(p.PermissionCode)));
        foreach (var code in permissions.Where(code => current.All(p => p.PermissionCode != code)))
            _context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = code });

        await _context.SaveChangesAsync(cancellationToken);
        _cache.InvalidateTenant(role.TenantId);
        await _audit.LogAsync("Roles.Updated", "Role", role.Id, cancellationToken: cancellationToken);

        return new Response(_localizer["Updated"].Value, HttpStatusCode.OK);
    }

    public async Task<Response> Delete(long id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken) ?? throw NotFound();
        if (role.IsSystemDefault)
            throw new BusinessException(_localizer["SystemRoleLocked"].Value, HttpStatusCode.BadRequest);

        var permissions = await _context.RolePermissions.Where(p => p.RoleId == id).ToListAsync(cancellationToken);
        EnsureCanGrant(permissions.Select(p => p.PermissionCode).ToList());

        _context.RolePermissions.RemoveRange(permissions);
        _context.MembershipRoles.RemoveRange(await _context.MembershipRoles.Where(r => r.RoleId == id).ToListAsync(cancellationToken));
        _context.Roles.Remove(role);

        await _context.SaveChangesAsync(cancellationToken);
        _cache.InvalidateTenant(role.TenantId);
        await _audit.LogAsync("Roles.Deleted", "Role", id, cancellationToken: cancellationToken);

        return new Response(_localizer["Deleted"].Value, HttpStatusCode.OK);
    }

    /// <summary>
    /// A user can't create or edit a role to hold permissions they don't have themselves,
    /// otherwise Roles.Manage would be a way to escalate privileges.
    /// </summary>
    private void EnsureCanGrant(IReadOnlyCollection<string> permissions)
    {
        if (permissions.Any(code => !Permissions.All.Contains(code)))
            throw new BusinessException(_localizer["InvalidPermission"].Value, HttpStatusCode.BadRequest);

        if (permissions.Any(code => !_access.HasPermission(code)))
            throw new BusinessException(_localizer["Forbidden"].Value, HttpStatusCode.Forbidden);
    }

    private IQueryable<RoleDto> ProjectRoles(IQueryable<Role> roles) =>
        roles.Select(r => new RoleDto
        {
            Id = r.Id,
            Name = r.Name,
            IsSystemDefault = r.IsSystemDefault,
            Permissions = _context.RolePermissions.Where(p => p.RoleId == r.Id).Select(p => p.PermissionCode).OrderBy(c => c).ToList(),
        });

    private BusinessException NotFound() => new(_localizer["RoleNotFound"].Value, HttpStatusCode.NotFound);
}
