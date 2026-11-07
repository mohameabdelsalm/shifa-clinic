using Shifa.Core.Interfaces.Common;

namespace Shifa.Infrastructure.Common.TenantManager;

/// <summary>Scoped, set-once holder of the request's permissions and branch scope.</summary>
public sealed class AccessContext : IAccessContext
{
    private bool _isSet;
    private bool _allBranches;
    private HashSet<long> _branchIds = [];
    private HashSet<string> _permissions = [];

    public bool IsPlatformAdmin { get; private set; }
    public long? MembershipId { get; private set; }

    public bool HasPermission(string permission) => _permissions.Contains(permission);

    public bool HasAllBranches => _allBranches;

    public IReadOnlyCollection<long> BranchIds => _branchIds;

    public bool CanAccessBranch(long branchId) => _allBranches || _branchIds.Contains(branchId);

    public void SetPlatformAdmin()
    {
        EnsureNotSet();
        IsPlatformAdmin = true;
    }

    public void SetMembership(long membershipId, bool allBranches, IEnumerable<long> branchIds, IEnumerable<string> permissions)
    {
        EnsureNotSet();
        MembershipId = membershipId;
        _allBranches = allBranches;
        _branchIds = [.. branchIds];
        _permissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }

    private void EnsureNotSet()
    {
        if (_isSet)
            throw new InvalidOperationException("The access context is already set.");
        _isSet = true;
    }
}
