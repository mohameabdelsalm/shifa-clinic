using Shifa.Core.Entities.Common;
using Shifa.Identity.Enums;

namespace Shifa.Identity.Entities;

/// <summary>A user's access to one tenant: status, roles (<see cref="MembershipRole"/>) and branch scope.</summary>
public class Membership : TenantOwnedEntity
{
    public long UserId { get; set; }

    /// <summary>When true the member works in every branch of the tenant; otherwise only the <see cref="MembershipBranch"/> rows.</summary>
    public bool AllBranches { get; set; }

    public MembershipStatusEnum Status { get; set; } = MembershipStatusEnum.Active;
}

public class Role : TenantOwnedEntity
{
    public string Name { get; set; } = "";

    /// <summary>Created at onboarding. System roles can't be deleted, and TenantAdmin can't be edited.</summary>
    public bool IsSystemDefault { get; set; }
}

public class RolePermission : TenantOwnedEntity
{
    public long RoleId { get; set; }

    /// <summary>A code from <see cref="Shifa.Core.Common.Permissions"/>.</summary>
    public string PermissionCode { get; set; } = "";
}

public class MembershipRole : TenantOwnedEntity
{
    public long MembershipId { get; set; }
    public long RoleId { get; set; }
}

public class MembershipBranch : TenantOwnedEntity
{
    public long MembershipId { get; set; }

    /// <summary>Reference to clinic.Branches by id (no cross-schema foreign key); validated through IClinicApi.</summary>
    public long BranchId { get; set; }
}
