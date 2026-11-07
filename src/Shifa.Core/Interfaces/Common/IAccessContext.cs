namespace Shifa.Core.Interfaces.Common;

/// <summary>
/// What the current request is allowed to do (ADR-007). Scoped and set once by TenantResolutionHandler
/// after it validated the token; permission and branch checks read from here.
/// Three kinds of caller: a Platform Admin, a staff member (membership with permissions and branches),
/// or a patient portal user (a verified phone number, no staff permissions).
/// </summary>
public interface IAccessContext
{
    bool IsPlatformAdmin { get; }

    /// <summary>A patient or guardian signed in with a phone OTP.</summary>
    bool IsPortalUser { get; }

    /// <summary>The verified phone number of a portal user.</summary>
    string? PortalPhoneNumber { get; }

    long? MembershipId { get; }
    bool HasPermission(string permission);

    /// <summary>True when the membership covers every branch of the tenant.</summary>
    bool HasAllBranches { get; }

    /// <summary>The branches the membership is limited to (empty when <see cref="HasAllBranches"/>).</summary>
    IReadOnlyCollection<long> BranchIds { get; }

    /// <summary>True when the membership covers all branches or lists this branch.</summary>
    bool CanAccessBranch(long branchId);

    void SetPlatformAdmin();
    void SetPortalUser(string phoneNumber);
    void SetMembership(long membershipId, bool allBranches, IEnumerable<long> branchIds, IEnumerable<string> permissions);
}
