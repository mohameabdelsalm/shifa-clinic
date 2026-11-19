using Core.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Identity;

public class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.Property(c => c.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(c => c.CodeHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(c => new { c.TenantId, c.PhoneNumber, c.CreationDate });
    }
}
