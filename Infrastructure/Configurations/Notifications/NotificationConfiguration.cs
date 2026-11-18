using Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Notifications;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Recipient).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Language).HasMaxLength(5).IsRequired();
        builder.Property(n => n.PayloadJson).HasMaxLength(4000).IsRequired();
        builder.Property(n => n.StatusReason).HasMaxLength(100);

        // The sending job reads what is due across clinics.
        builder.HasIndex(n => new { n.Status, n.ScheduledAt });
        builder.HasIndex(n => new { n.TenantId, n.AppointmentId });
        builder.HasIndex(n => new { n.TenantId, n.PatientId });
        builder.HasIndex(n => new { n.TenantId, n.ScheduledAt });
    }
}
