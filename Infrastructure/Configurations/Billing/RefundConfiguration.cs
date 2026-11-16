using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.Property(r => r.Number).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(500).IsRequired();
        builder.Property(r => r.ExternalReference).HasMaxLength(100);

        builder.HasIndex(r => new { r.TenantId, r.Number }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(r => new { r.TenantId, r.Year, r.Sequence }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.References<Refund, Payment>(nameof(Refund.PaymentId));
        builder.References<Refund, CashierShift>(nameof(Refund.CashierShiftId));
    }
}
