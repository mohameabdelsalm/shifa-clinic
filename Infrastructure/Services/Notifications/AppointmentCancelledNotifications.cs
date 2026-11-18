using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

public sealed class AppointmentCancelledNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<AppointmentCancelled, NotificationsDbContext>(context, time)
{
    protected override async Task HandleCoreAsync(AppointmentCancelled e, CancellationToken cancellationToken)
    {
        await planner.CancelScheduledAsync(e.AppointmentId, "AppointmentCancelled", cancellationToken);
        await planner.PlanAppointmentAsync(e, NotificationKindEnum.AppointmentCancelled, withReminders: false, cancellationToken);
    }
}
