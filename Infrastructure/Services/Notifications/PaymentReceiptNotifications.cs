using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Infrastructure.Data;
using Infrastructure.Services.Messaging;

namespace Infrastructure.Services.Notifications;

public sealed class PaymentReceiptNotifications(NotificationsDbContext context, TimeProvider time, NotificationPlanner planner)
    : InboxEventHandler<PaymentRecorded, NotificationsDbContext>(context, time)
{
    protected override Task HandleCoreAsync(PaymentRecorded e, CancellationToken cancellationToken) => planner.PlanReceiptAsync(e, cancellationToken);
}
