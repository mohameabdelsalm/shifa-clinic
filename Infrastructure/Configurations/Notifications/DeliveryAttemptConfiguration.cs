using Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Notifications;

public class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.Property(a => a.Provider).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Error).HasMaxLength(200);
        builder.HasIndex(a => new { a.TenantId, a.NotificationId });

        // Composite tenant FK: an attempt can only belong to a notification of the same clinic.
        builder.HasOne<Notification>().WithMany()
            .HasForeignKey("TenantId", nameof(DeliveryAttempt.NotificationId))
            .HasPrincipalKey("TenantId", "Id")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
