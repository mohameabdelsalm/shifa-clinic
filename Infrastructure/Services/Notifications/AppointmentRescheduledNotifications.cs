using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

/// <summary>Old reminders are cancelled; a confirmed appointment gets a "moved" message and new reminders.</summary>
public sealed class AppointmentRescheduledNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<AppointmentRescheduled, NotificationsDbContext>(context, time)
{
    protected override async Task HandleCoreAsync(AppointmentRescheduled e, CancellationToken cancellationToken)
    {
        await planner.CancelScheduledAsync(e.AppointmentId, "Rescheduled", cancellationToken);
        if (e.Confirmed)
            await planner.PlanAppointmentAsync(e, NotificationKindEnum.AppointmentRescheduled, withReminders: true, cancellationToken);
    }
}
