using Core.Entities.Clinical;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinical;

public class PatientConditionConfiguration : IEntityTypeConfiguration<PatientCondition>
{
    public void Configure(EntityTypeBuilder<PatientCondition> builder)
    {
        builder.Property(c => c.Condition).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(500);
        builder.HasIndex(c => new { c.TenantId, c.PatientId });
    }
}
