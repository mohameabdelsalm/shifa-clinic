using Core.Entities.Common;

namespace Core.Entities.Clinic;

/// <summary>Where a doctor works. A doctor can work in several branches of the same tenant.</summary>
public class DoctorBranch : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
}
