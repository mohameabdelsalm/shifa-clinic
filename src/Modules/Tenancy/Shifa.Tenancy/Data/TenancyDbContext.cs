using Microsoft.EntityFrameworkCore;
using Shifa.Core.Interfaces.Common;
using Shifa.Infrastructure.Data;
using Shifa.Tenancy.Entities;

namespace Shifa.Tenancy.Data;

public class TenancyDbContext : ModuleDbContext
{
    public const string SchemaName = "tenancy";

    public TenancyDbContext(DbContextOptions<TenancyDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<PlanEntitlement> PlanEntitlements => Set<PlanEntitlement>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<SubscriptionPayment> SubscriptionPayments => Set<SubscriptionPayment>();
}
