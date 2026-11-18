using Core.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations.Notifications;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.Property(t => t.Language).HasMaxLength(5).IsRequired();
        builder.Property(t => t.Body).HasMaxLength(600).IsRequired();

        // One custom text per clinic, kind, channel and language.
        builder.HasIndex(t => new { t.TenantId, t.Kind, t.Channel, t.Language }).IsUnique().HasFilter("[IsDeleted] = 0");
    }
}
