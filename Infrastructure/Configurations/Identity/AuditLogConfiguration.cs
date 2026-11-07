using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Entity).HasMaxLength(100);
        builder.Property(a => a.Detail).HasMaxLength(1000);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.HasIndex(a => new { a.TargetTenantId, a.CreationDate });
        builder.HasIndex(a => new { a.UserId, a.CreationDate });
    }
}
