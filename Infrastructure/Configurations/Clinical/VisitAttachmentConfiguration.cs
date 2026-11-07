using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class VisitAttachmentConfiguration : IEntityTypeConfiguration<VisitAttachment>
{
    public void Configure(EntityTypeBuilder<VisitAttachment> builder)
    {
        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
        builder.ReferencesVisit();
    }
}
