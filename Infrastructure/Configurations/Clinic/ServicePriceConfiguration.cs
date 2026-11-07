using Core.Entities.Clinic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Clinic;

public class ServicePriceConfiguration : IEntityTypeConfiguration<ServicePrice>
{
    public void Configure(EntityTypeBuilder<ServicePrice> builder)
    {
        builder.References<ServicePrice, Service>(x => x.ServiceId);
        builder.References<ServicePrice, Branch>(x => x.BranchId);

        // At most one open (current) price per service and branch.
        builder.HasIndex(x => new { x.TenantId, x.ServiceId, x.BranchId }).IsUnique()
            .HasFilter("[EffectiveTo] IS NULL AND [IsDeleted] = 0");
    }
}
