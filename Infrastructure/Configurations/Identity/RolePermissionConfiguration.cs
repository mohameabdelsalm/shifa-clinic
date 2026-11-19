using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.Property(p => p.PermissionCode).HasMaxLength(100).IsRequired();

        // Composite FK (TenantId, RoleId) -> (TenantId, Id): a permission can't point to another tenant's role.
        builder.HasOne<Role>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.RoleId })
            .HasPrincipalKey(r => new { r.TenantId, r.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.RoleId, p.PermissionCode }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
