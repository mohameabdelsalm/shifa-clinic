using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shifa.Tenancy.Entities;

namespace Shifa.Tenancy.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(100).IsRequired();
        builder.Property(t => t.DefaultTimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}
