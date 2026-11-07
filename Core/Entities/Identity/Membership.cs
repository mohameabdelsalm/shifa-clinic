using Core.Entities.Common;
using Core.Enums.Identity;

namespace Core.Entities.Identity;

/// <summary>A user's access to one tenant: status, roles (<see cref="MembershipRole"/>) and branch scope.</summary>
public class Membership : TenantOwnedEntity
{
    public long UserId { get; set; }

    /// <summary>When true the member works in every branch of the tenant; otherwise only the <see cref="MembershipBranch"/> rows.</summary>
    public bool AllBranches { get; set; }

    public MembershipStatusEnum Status { get; set; } = MembershipStatusEnum.Active;
}
