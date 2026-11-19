using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class PasswordTokenConfiguration : IEntityTypeConfiguration<PasswordToken>
{
    public void Configure(EntityTypeBuilder<PasswordToken> builder)
    {
        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.Purpose });
    }
}
