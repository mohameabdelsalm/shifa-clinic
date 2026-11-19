using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.Property(i => i.DrugName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Dosage).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Instructions).HasMaxLength(500);

        // Composite tenant FK (TenantId, PrescriptionId) -> Prescriptions (TenantId, Id).
        builder.HasOne<Prescription>().WithMany()
            .HasForeignKey("TenantId", nameof(PrescriptionItem.PrescriptionId))
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
