namespace Shifa.Identity.Contracts;

/// <summary>What a membership may do. Null from <see cref="IIdentityApi.GetMembershipAccessAsync"/> means "not active".</summary>
public sealed record MembershipAccess(
    long TenantId,
    long UserId,
    long MembershipId,
    bool AllBranches,
    IReadOnlyList<long> BranchIds,
    IReadOnlyList<string> Permissions);

public sealed record ProvisionTenantRequest(long TenantId, string AdminFullName, string AdminEmail, string AdminPassword);

public sealed record ProvisionTenantResult(long AdminUserId, long MembershipId);

public interface IIdentityApi
{
    /// <summary>Active membership of <paramref name="userId"/> in the tenant, with merged role permissions; cached briefly.</summary>
    Task<MembershipAccess?> GetMembershipAccessAsync(long tenantId, long membershipId, long userId, CancellationToken cancellationToken = default);

    /// <summary>True when the user exists and is active (portal sessions have no membership to check); cached briefly.</summary>
    Task<bool> IsActiveUserAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>True when the user exists, is active and is a Platform Admin; cached briefly.</summary>
    Task<bool> IsActivePlatformAdminAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Onboarding step: creates the default roles of a new tenant and its first TenantAdmin membership.
    /// Runs in the new tenant's own scope. An existing user (same email) is linked, its password is not changed.
    /// </summary>
    Task<ProvisionTenantResult> ProvisionTenantAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Active memberships in the tenant; used for the plan's MaxEmployees limit.</summary>
    Task<int> CountActiveMembershipsAsync(long tenantId, CancellationToken cancellationToken = default);
}
