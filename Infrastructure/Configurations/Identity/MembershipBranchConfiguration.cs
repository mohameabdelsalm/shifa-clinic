using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class MembershipBranchConfiguration : IEntityTypeConfiguration<MembershipBranch>
{
    public void Configure(EntityTypeBuilder<MembershipBranch> builder)
    {
        builder.HasOne<Membership>().WithMany()
            .HasForeignKey(x => new { x.TenantId, x.MembershipId })
            .HasPrincipalKey(m => new { m.TenantId, m.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.TenantId, x.MembershipId, x.BranchId }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
