namespace Shifa.Core.Interfaces.Common;

/// <summary>
/// What the current request is allowed to do (ADR-007). Scoped and set once by TenantResolutionHandler
/// after it validated the membership; permission and branch checks read from here.
/// </summary>
public interface IAccessContext
{
    bool IsPlatformAdmin { get; }
    long? MembershipId { get; }
    bool HasPermission(string permission);

    /// <summary>True when the membership covers every branch of the tenant.</summary>
    bool HasAllBranches { get; }

    /// <summary>The branches the membership is limited to (empty when <see cref="HasAllBranches"/>).</summary>
    IReadOnlyCollection<long> BranchIds { get; }

    /// <summary>True when the membership covers all branches or lists this branch.</summary>
    bool CanAccessBranch(long branchId);

    void SetPlatformAdmin();
    void SetMembership(long membershipId, bool allBranches, IEnumerable<long> branchIds, IEnumerable<string> permissions);
}
