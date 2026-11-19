using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

public sealed class AppointmentConfirmedNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<AppointmentConfirmed, NotificationsDbContext>(context, time)
{
    protected override Task HandleCoreAsync(AppointmentConfirmed e, CancellationToken cancellationToken) =>
        planner.PlanAppointmentAsync(e, NotificationKindEnum.AppointmentConfirmed, withReminders: true, cancellationToken);
}
