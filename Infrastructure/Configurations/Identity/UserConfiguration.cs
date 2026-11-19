using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();

        // Optional but unique when present: SQL Server treats NULL as a value in unique indexes.
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");
        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL AND [IsDeleted] = 0");
    }
}
