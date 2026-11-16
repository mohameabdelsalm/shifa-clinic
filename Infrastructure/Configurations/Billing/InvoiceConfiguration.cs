using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Ignore(i => i.Balance);
        builder.Property(i => i.RowVersion).IsRowVersion();
        builder.Property(i => i.Number).HasMaxLength(20).IsRequired();
        builder.Property(i => i.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(i => i.VoidReason).HasMaxLength(500);

        builder.HasIndex(i => new { i.TenantId, i.Number }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(i => new { i.TenantId, i.Year, i.Sequence }).IsUnique().HasFilter("[IsDeleted] = 0");

        // At most one visit invoice per appointment, also when check-in and the repair endpoint race.
        builder.HasIndex(i => new { i.TenantId, i.AppointmentId })
            .IsUnique()
            .HasFilter("[Kind] = 'Visit' AND [IsDeleted] = 0")
            .HasDatabaseName(BillingIndexes.VisitInvoice);

        builder.HasIndex(i => new { i.TenantId, i.BranchId, i.IssuedAt });
        builder.HasIndex(i => new { i.TenantId, i.PatientId, i.IssuedAt });
    }
}
