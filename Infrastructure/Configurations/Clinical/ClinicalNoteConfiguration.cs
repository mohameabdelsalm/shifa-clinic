using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.Property(n => n.ChiefComplaint).HasMaxLength(500);
        builder.Property(n => n.Notes).HasMaxLength(4000);
        builder.HasIndex(n => new { n.TenantId, n.VisitId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ReferencesVisit();
    }
}
