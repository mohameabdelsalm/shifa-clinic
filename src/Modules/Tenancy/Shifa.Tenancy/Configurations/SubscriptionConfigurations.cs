using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shifa.Tenancy.Entities;

namespace Shifa.Tenancy.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(p => p.Code).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class PlanEntitlementConfiguration : IEntityTypeConfiguration<PlanEntitlement>
{
    public void Configure(EntityTypeBuilder<PlanEntitlement> builder)
    {
        builder.Property(e => e.Code).HasMaxLength(100).IsRequired();
        builder.HasOne<Plan>().WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.PlanId, e.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}

public class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Plan>().WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.TenantId, s.StartsAt });
    }
}

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.Property(p => p.CurrencyCode).HasMaxLength(3).IsFixedLength().IsRequired();
        builder.Property(p => p.Reference).HasMaxLength(200);
        builder.HasOne<TenantSubscription>().WithMany().HasForeignKey(p => p.TenantSubscriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
