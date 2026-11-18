using Core.Entities.Billing;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class BillingDbContext : ModuleDbContext
{
    public const string SchemaName = "billing";

    public BillingDbContext(DbContextOptions<BillingDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    protected override string Module => "Billing";

    public override bool HasOutbox => true;

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CashierShift> CashierShifts => Set<CashierShift>();
    public DbSet<BillingIdempotencyRecord> IdempotencyRecords => Set<BillingIdempotencyRecord>();
    public DbSet<PaymentGatewaySettings> PaymentGatewaySettings => Set<PaymentGatewaySettings>();
    public DbSet<OnlinePaymentAttempt> OnlinePaymentAttempts => Set<OnlinePaymentAttempt>();
}
