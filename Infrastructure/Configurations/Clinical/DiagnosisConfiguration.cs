using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class DiagnosisConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("Diagnoses");
        builder.Property(d => d.Icd10Code).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(300);
        builder.HasIndex(d => new { d.TenantId, d.Icd10Code });
        builder.ReferencesVisit();
    }
}
