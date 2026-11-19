using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.Property(c => c.Number).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(500).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Number }).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(c => new { c.TenantId, c.Year, c.Sequence }).IsUnique().HasFilter("[IsDeleted] = 0");
        // Money statistics by date.
        builder.HasIndex(c => new { c.TenantId, c.CreationDate });
        builder.References<CreditNote, Invoice>(nameof(CreditNote.InvoiceId));
    }
}
