using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class DoctorBranchConfiguration : IEntityTypeConfiguration<DoctorBranch>
{
    public void Configure(EntityTypeBuilder<DoctorBranch> builder)
    {
        builder.References<DoctorBranch, Doctor>(x => x.DoctorId);
        builder.References<DoctorBranch, Branch>(x => x.BranchId);
        builder.HasIndex(x => new { x.TenantId, x.DoctorId, x.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => new { x.TenantId, x.BranchId });
    }
}
