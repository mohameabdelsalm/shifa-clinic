using Core.Entities.Notifications;
using Core.Interfaces.Common;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>Messages to patients (ADR-015). Consumes Scheduling and Billing events, so it has an inbox.</summary>
public class NotificationsDbContext : ModuleDbContext
{
    public const string SchemaName = "notifications";

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public override string Schema => SchemaName;

    protected override string Module => "Notifications";

    public override bool HasInbox => true;

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeliveryAttempt> DeliveryAttempts => Set<DeliveryAttempt>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
}
