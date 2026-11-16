using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Ignore(p => p.Refundable);
        builder.Property(p => p.Number).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(100);

        builder.HasIndex(p => new { p.TenantId, p.Number }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(p => new { p.TenantId, p.Year, p.Sequence }).IsUnique().HasFilter("[IsDeleted] = 0");

        // A gateway transaction is recorded once, even if its callback arrives twice.
        builder.HasIndex(p => new { p.TenantId, p.Reference })
            .IsUnique()
            .HasFilter("[Method] = 'Online' AND [IsDeleted] = 0")
            .HasDatabaseName(BillingIndexes.OnlineTransaction);

        builder.HasIndex(p => new { p.TenantId, p.PaidAt });
        builder.HasIndex(p => new { p.TenantId, p.InvoiceId });
        builder.References<Payment, Invoice>(nameof(Payment.InvoiceId));
        builder.References<Payment, CashierShift>(nameof(Payment.CashierShiftId));
    }
}
