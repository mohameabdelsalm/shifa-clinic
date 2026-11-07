using Core.Common;
using Core.DTO.Identity;
using Core.Entities.Identity;
using Core.Enums.Common;
using Core.Enums.Identity;
using Core.Interfaces.Common;
using Core.Interfaces.Identity;
using Infrastructure.Data;
using Infrastructure.Services.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Repositories.Identity;

/// <summary>
/// The public face of the Identity module (Identity.Contracts). Every tenant-scoped lookup runs inside
/// <see cref="ITenantScopeFactory"/> with the tenant from the validated token, so the Tenant query filter applies.
/// </summary>
public sealed class IdentityApi : IIdentityApi
{
    private readonly IdentityDbContext _context;
    private readonly ITenantScopeFactory _scopes;
    private readonly MembershipAccessCache _cache;
    private readonly IPasswordHasher<User> _hasher;

    public IdentityApi(IdentityDbContext context, ITenantScopeFactory scopes, MembershipAccessCache cache, IPasswordHasher<User> hasher)
    {
        _context = context;
        _scopes = scopes;
        _cache = cache;
        _hasher = hasher;
    }

    public async Task<MembershipAccess?> GetMembershipAccessAsync(long tenantId, long membershipId, long userId, CancellationToken cancellationToken = default)
    {
        var cached = _cache.GetMembership(tenantId, membershipId);
        if (cached is not null)
            return cached.UserId == userId ? cached : null;

        var access = await _scopes.RunAsync(tenantId, async sp =>
        {
            var db = sp.GetRequiredService<IdentityDbContext>();

            var membership = await db.Memberships.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == membershipId && m.UserId == userId && m.Status == MembershipStatusEnum.Active, cancellationToken);
            if (membership is null || !await db.Users.AnyAsync(u => u.Id == userId && u.Status == StatusEnum.Active, cancellationToken))
                return null;

            var permissions = await (
                from mr in db.MembershipRoles
                where mr.MembershipId == membershipId
                join r in db.Roles on mr.RoleId equals r.Id
                join p in db.RolePermissions on r.Id equals p.RoleId
                select p.PermissionCode).Distinct().ToListAsync(cancellationToken);

            var branchIds = await db.MembershipBranches.Where(b => b.MembershipId == membershipId).Select(b => b.BranchId).ToListAsync(cancellationToken);

            return new MembershipAccess(tenantId, userId, membershipId, membership.AllBranches, branchIds, permissions);
        });

        if (access is not null)
            _cache.SetMembership(access);
        return access;
    }

    public async Task<bool> IsActiveUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var cached = _cache.GetActiveUser(userId);
        if (cached is not null)
            return cached.Value;

        var isActive = await _context.Users.AnyAsync(u => u.Id == userId && u.Status == StatusEnum.Active, cancellationToken);
        _cache.SetActiveUser(userId, isActive);
        return isActive;
    }

    public async Task<bool> IsActivePlatformAdminAsync(long userId, CancellationToken cancellationToken = default)
    {
        var cached = _cache.GetPlatformAdmin(userId);
        if (cached is not null)
            return cached.Value;

        var isAdmin = await _context.Users.AnyAsync(u => u.Id == userId && u.IsPlatformAdmin && u.Status == StatusEnum.Active, cancellationToken);
        _cache.SetPlatformAdmin(userId, isAdmin);
        return isAdmin;
    }

    public Task<ProvisionTenantResult> ProvisionTenantAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default) =>
        _scopes.RunAsync(request.TenantId, async sp =>
        {
            var db = sp.GetRequiredService<IdentityDbContext>();
            long adminRoleId = 0;

            foreach (var (name, permissions) in Permissions.DefaultRoles)
            {
                var role = new Role { Name = name, IsSystemDefault = true };
                db.Roles.Add(role);
                foreach (var code in permissions)
                    db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = code });

                if (name == Permissions.TenantAdminRole)
                    adminRoleId = role.Id;
            }

            var email = request.AdminEmail.Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user is null)
            {
                user = new User { FullName = request.AdminFullName.Trim(), Email = email };
                user.PasswordHash = _hasher.HashPassword(user, request.AdminPassword);
                db.Users.Add(user);
            }

            var membership = new Membership { UserId = user.Id, AllBranches = true };
            db.Memberships.Add(membership);
            db.MembershipRoles.Add(new MembershipRole { MembershipId = membership.Id, RoleId = adminRoleId });

            await db.SaveChangesAsync(cancellationToken);
            return new ProvisionTenantResult(user.Id, membership.Id);
        });

    public Task<int> CountActiveMembershipsAsync(long tenantId, CancellationToken cancellationToken = default) =>
        _scopes.RunAsync(tenantId, sp => sp.GetRequiredService<IdentityDbContext>().Memberships
            .CountAsync(m => m.Status == MembershipStatusEnum.Active, cancellationToken));
}
