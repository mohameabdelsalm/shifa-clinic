using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Code).HasMaxLength(50).IsRequired();
        builder.Property(b => b.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(b => b.Phone).HasMaxLength(30);
        builder.Property(b => b.Address).HasMaxLength(500);

        // Unique inside a tenant, not globally: two tenants may both have a "MAIN" branch.
        builder.HasIndex(b => new { b.TenantId, b.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(b => new { b.TenantId, b.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
