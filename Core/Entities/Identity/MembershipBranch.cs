using Core.Entities.Common;

namespace Core.Entities.Identity;

public class MembershipBranch : TenantOwnedEntity
{
    public long MembershipId { get; set; }

    /// <summary>Reference to clinic.Branches by id (no cross-schema foreign key); validated through IClinicApi.</summary>
    public long BranchId { get; set; }
}
