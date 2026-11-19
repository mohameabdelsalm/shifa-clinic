using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

/// <summary>Confirmed booking → confirmation now + reminders. A Pending booking waits for AppointmentConfirmed.</summary>
public sealed class AppointmentBookedNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<AppointmentBooked, NotificationsDbContext>(context, time)
{
    protected override Task HandleCoreAsync(AppointmentBooked e, CancellationToken cancellationToken) =>
        e.Confirmed ? planner.PlanAppointmentAsync(e, NotificationKindEnum.AppointmentConfirmed, withReminders: true, cancellationToken) : Task.CompletedTask;
}
