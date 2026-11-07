using Core.Entities.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Tenancy;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.Property(t => t.DefaultTimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();

        // Unique among live tenants. A tenant removed by a failed onboarding (soft delete) must not keep its slug forever;
        // a new tenant with the same slug gets a new Id, so it never sees the old tenant's rows.
        builder.HasIndex(t => t.Slug).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
