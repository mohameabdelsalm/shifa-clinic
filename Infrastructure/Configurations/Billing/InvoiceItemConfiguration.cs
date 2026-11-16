using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.Property(i => i.Description).HasMaxLength(300).IsRequired();
        builder.References<InvoiceItem, Invoice>(nameof(InvoiceItem.InvoiceId));
    }
}
