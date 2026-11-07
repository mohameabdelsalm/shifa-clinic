using Core.Entities.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Scheduling;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.Property(r => r.Operation).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Operation, r.Key })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(SchedulingIndexes.Idempotency);
    }
}
