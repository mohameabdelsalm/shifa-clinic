using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

/// <summary>The patient arrived: remaining reminders are pointless.</summary>
public sealed class PatientCheckedInNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<PatientCheckedIn, NotificationsDbContext>(context, time)
{
    protected override Task HandleCoreAsync(PatientCheckedIn e, CancellationToken cancellationToken) =>
        planner.CancelScheduledAsync(e.AppointmentId, "CheckedIn", cancellationToken);
}
