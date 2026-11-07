using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.Property(a => a.Allergen).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Reaction).HasMaxLength(500);
        builder.HasIndex(a => new { a.TenantId, a.PatientId });
    }
}
