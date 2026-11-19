using Core.Interfaces.Identity;

namespace Core.DTO.Identity;

/// <summary>What a membership may do. Null from <see cref="IIdentityApi.GetMembershipAccessAsync"/> means "not active".</summary>
public sealed record MembershipAccess(
    long TenantId,
    long UserId,
    long MembershipId,
    bool AllBranches,
    IReadOnlyList<long> BranchIds,
    IReadOnlyList<string> Permissions);
