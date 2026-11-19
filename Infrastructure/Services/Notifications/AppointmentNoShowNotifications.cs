using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

public sealed class AppointmentNoShowNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<AppointmentNoShow, NotificationsDbContext>(context, time)
{
    protected override Task HandleCoreAsync(AppointmentNoShow e, CancellationToken cancellationToken) =>
        planner.CancelScheduledAsync(e.AppointmentId, "NoShow", cancellationToken);
}
