using System.Text.Json;
using Core.DTO.Tenancy;
using Core.Entities.Notifications;
using Core.Enums.Notifications;
using Core.Enums.Scheduling;
using Core.Interfaces.Common;
using Core.Interfaces.Scheduling;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Notifications;

/// <summary>
/// Sends due notifications of the current tenant (called by the notifications job inside the tenant's scope).
/// Before an appointment message it checks the appointment is still confirmed at the same time; a provider failure is
/// retried <see cref="Notification.MaxAttempts"/> times. Logs carry the notification id only, never text or recipient.
/// </summary>
public sealed class NotificationSender
{
    private readonly NotificationsDbContext _context;
    private readonly NotificationTemplates _templates;
    private readonly ISchedulingApi _scheduling;
    private readonly ITenancyApi _tenancy;
    private readonly ITenantContext _tenant;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly TimeProvider _time;
    private readonly NotificationOptions _options;
    private readonly ILogger<NotificationSender> _logger;

    public NotificationSender(NotificationsDbContext context, NotificationTemplates templates, ISchedulingApi scheduling, ITenancyApi tenancy,
        ITenantContext tenant, ISmsSender sms, IEmailSender email, TimeProvider time, IOptions<NotificationOptions> options, ILogger<NotificationSender> logger)
    {
        _context = context;
        _templates = templates;
        _scheduling = scheduling;
        _tenancy = tenancy;
        _tenant = tenant;
        _sms = sms;
        _email = email;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> SendDueAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("No tenant.");
        var tenant = await _tenancy.GetTenantAsync(tenantId, cancellationToken);
        var enabled = await _tenancy.IsFeatureEnabledAsync(tenantId, FeatureCodes.Notifications, cancellationToken);

        var due = await _context.Notifications
            .Where(n => ids.Contains(n.Id) && n.Status == NotificationStatusEnum.Scheduled)
            .OrderBy(n => n.ScheduledAt)
            .ToListAsync(cancellationToken);

        var sent = 0;
        foreach (var notification in due)
        {
            var now = _time.GetUtcNow();
            if (tenant is not { IsActive: true })
                Close(notification, NotificationStatusEnum.Skipped, "ClinicInactive");
            else if (!enabled)
                Close(notification, NotificationStatusEnum.Skipped, "FeatureOff");
            else if (notification.Channel == NotificationChannelEnum.Sms && !_sms.IsEnabled)
                Close(notification, NotificationStatusEnum.Skipped, "SmsDisabled");
            else if (await StaleReasonAsync(notification, now, cancellationToken) is { } stale)
                Close(notification, stale.Status, stale.Reason);
            else if (await TrySendAsync(notification, now, cancellationToken))
                sent++;

            await _context.SaveChangesAsync(CancellationToken.None);
        }

        return sent;
    }

    /// <summary>Appointment messages are only valid while the appointment is confirmed at the planned time and still ahead.</summary>
    private async Task<(NotificationStatusEnum Status, string Reason)?> StaleReasonAsync(Notification n, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (n.AppointmentId is not long appointmentId || n.Kind is NotificationKindEnum.AppointmentCancelled or NotificationKindEnum.PaymentReceipt)
            return null;

        var state = await _scheduling.GetAppointmentStateAsync(appointmentId, cancellationToken);
        if (state is null || state.Status != AppointmentStatusEnum.Confirmed || state.StartAt != n.AppointmentStartAt)
            return (NotificationStatusEnum.Cancelled, "AppointmentChanged");
        if (state.StartAt <= now)
            return (NotificationStatusEnum.Skipped, "TooLate");
        return null;
    }

    private async Task<bool> TrySendAsync(Notification n, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(n.PayloadJson) ?? [];
        var text = await _templates.RenderAsync(n.Kind, n.Channel, n.Language, values, cancellationToken);
        var provider = n.Channel == NotificationChannelEnum.Sms ? _sms.GetType().Name : _email.GetType().Name;
        n.Attempts++;

        try
        {
            if (n.Channel == NotificationChannelEnum.Sms)
                await _sms.SendAsync(n.Recipient, text, cancellationToken);
            else
                await _email.SendAsync(n.Recipient, _templates.Subject(n.Kind, n.Language), text, cancellationToken);

            n.Status = NotificationStatusEnum.Sent;
            n.SentAt = now;
            _context.DeliveryAttempts.Add(new DeliveryAttempt { NotificationId = n.Id, Provider = provider, Succeeded = true, AttemptedAt = now });
            return true;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _context.DeliveryAttempts.Add(new DeliveryAttempt
            {
                NotificationId = n.Id, Provider = provider, Succeeded = false, Error = ex.GetType().Name, AttemptedAt = now,
            });

            if (n.Attempts >= Notification.MaxAttempts)
            {
                Close(n, NotificationStatusEnum.Failed, "ProviderFailed");
                _logger.LogError("Notification {NotificationId} failed after {Attempts} attempts ({Error})", n.Id, n.Attempts, ex.GetType().Name);
            }
            else
            {
                n.ScheduledAt = now + _options.RetryDelay;
                _logger.LogWarning("Notification {NotificationId} attempt {Attempts} failed ({Error}); retrying", n.Id, n.Attempts, ex.GetType().Name);
            }
            return false;
        }
    }

    private static void Close(Notification n, NotificationStatusEnum status, string reason)
    {
        n.Status = status;
        n.StatusReason = reason;
    }
}
