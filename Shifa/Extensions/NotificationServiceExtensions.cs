using Core.Events.Billing;
using Core.Events.Scheduling;
using Core.Interfaces.Notifications;
using Infrastructure.Data;
using Infrastructure.Repositories.Notifications;
using Infrastructure.Services.Messaging;
using Infrastructure.Services.Notifications;

namespace Shifa.Extensions;

public static class NotificationServiceExtensions
{
    /// <summary>The outbox dispatcher, the Notifications module and its event handlers (ADR-015).</summary>
    public static IServiceCollection AddShifaMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOutbox(configuration, typeof(SchedulingDbContext), typeof(BillingDbContext));

        services.Configure<NotificationOptions>(configuration.GetSection(NotificationOptions.Section));
        services.AddScoped<NotificationTemplates>();
        services.AddScoped<NotificationPlanner>();
        services.AddScoped<NotificationSender>();
        services.AddScoped<INotificationRepo, NotificationRepo>();

        services.AddIntegrationEventHandler<AppointmentBooked, AppointmentBookedNotifications>();
        services.AddIntegrationEventHandler<AppointmentConfirmed, AppointmentConfirmedNotifications>();
        services.AddIntegrationEventHandler<AppointmentRescheduled, AppointmentRescheduledNotifications>();
        services.AddIntegrationEventHandler<AppointmentCancelled, AppointmentCancelledNotifications>();
        services.AddIntegrationEventHandler<PatientCheckedIn, PatientCheckedInNotifications>();
        services.AddIntegrationEventHandler<AppointmentNoShow, AppointmentNoShowNotifications>();
        services.AddIntegrationEventHandler<PaymentRecorded, PaymentReceiptNotifications>();
        return services;
    }
}
