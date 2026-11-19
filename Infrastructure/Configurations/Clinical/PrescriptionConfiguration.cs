using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.HasIndex(p => new { p.TenantId, p.VisitId }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.ReferencesVisit();
    }
}
