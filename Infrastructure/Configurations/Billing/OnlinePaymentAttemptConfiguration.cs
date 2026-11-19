using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class OnlinePaymentAttemptConfiguration : IEntityTypeConfiguration<OnlinePaymentAttempt>
{
    public void Configure(EntityTypeBuilder<OnlinePaymentAttempt> builder)
    {
        builder.Property(a => a.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(a => a.Reference).HasMaxLength(64).IsRequired();
        builder.Property(a => a.GatewayIntentionId).HasMaxLength(100);
        builder.Property(a => a.GatewayTransactionId).HasMaxLength(100);
        builder.Property(a => a.GatewayOrderId).HasMaxLength(100);

        // The callback finds the attempt by our reference (merchant_order_id).
        builder.HasIndex(a => new { a.TenantId, a.Reference }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(a => new { a.TenantId, a.InvoiceId });
        builder.HasIndex(a => new { a.TenantId, a.Status });
        // The expiry job looks across clinics for old Pending attempts.
        builder.HasIndex(a => new { a.Status, a.CreationDate });
        builder.References<OnlinePaymentAttempt, Invoice>(nameof(OnlinePaymentAttempt.InvoiceId));
        builder.References<OnlinePaymentAttempt, Payment>(nameof(OnlinePaymentAttempt.PaymentId));
    }
}
