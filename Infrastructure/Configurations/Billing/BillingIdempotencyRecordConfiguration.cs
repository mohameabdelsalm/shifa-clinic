using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class BillingIdempotencyRecordConfiguration : IEntityTypeConfiguration<BillingIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<BillingIdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.Property(r => r.Operation).HasMaxLength(60).IsRequired();
        builder.Property(r => r.Key).HasMaxLength(100).IsRequired();
        builder.Property(r => r.RequestHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.TenantId, r.Operation, r.Key })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName(BillingIndexes.Idempotency);
    }
}
