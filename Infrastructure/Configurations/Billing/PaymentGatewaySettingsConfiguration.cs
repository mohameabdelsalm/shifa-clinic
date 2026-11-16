using Core.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Billing;

public class PaymentGatewaySettingsConfiguration : IEntityTypeConfiguration<PaymentGatewaySettings>
{
    public void Configure(EntityTypeBuilder<PaymentGatewaySettings> builder)
    {
        builder.Property(s => s.Provider).HasMaxLength(30).IsRequired();
        builder.Property(s => s.PublicKey).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SecretKeyProtected).HasMaxLength(2000).IsRequired();
        builder.Property(s => s.HmacSecretProtected).HasMaxLength(2000).IsRequired();
        builder.Property(s => s.IntegrationIds).HasMaxLength(200).IsRequired();

        // One gateway configuration per clinic.
        builder.HasIndex(s => s.TenantId).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
