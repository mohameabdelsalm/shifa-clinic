using Core.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Tenancy;

public class TenantFeatureOverrideConfiguration : IEntityTypeConfiguration<TenantFeatureOverride>
{
    public void Configure(EntityTypeBuilder<TenantFeatureOverride> builder)
    {
        builder.Property(o => o.Code).HasMaxLength(100).IsRequired();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => new { o.TenantId, o.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
