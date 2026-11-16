using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class CashierShiftConfiguration : IEntityTypeConfiguration<CashierShift>
{
    public void Configure(EntityTypeBuilder<CashierShift> builder)
    {
        builder.Ignore(s => s.IsOpen);
        builder.Property(s => s.RowVersion).IsRowVersion();
        builder.Property(s => s.Notes).HasMaxLength(500);

        // One open shift per cashier and branch.
        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.CashierUserId })
            .IsUnique()
            .HasFilter("[Status] = 'Open' AND [IsDeleted] = 0")
            .HasDatabaseName(BillingIndexes.OpenShift);

        builder.HasIndex(s => new { s.TenantId, s.BranchId, s.OpenedAt });
    }
}
