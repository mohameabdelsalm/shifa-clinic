using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class ClinicalAddendumConfiguration : IEntityTypeConfiguration<ClinicalAddendum>
{
    public void Configure(EntityTypeBuilder<ClinicalAddendum> builder)
    {
        builder.ToTable("ClinicalAddenda");
        builder.Property(a => a.Reason).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Content).HasMaxLength(4000).IsRequired();
        builder.ReferencesVisit();
    }
}
