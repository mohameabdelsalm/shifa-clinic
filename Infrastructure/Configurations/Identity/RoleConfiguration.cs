using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.Property(r => r.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Name }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
