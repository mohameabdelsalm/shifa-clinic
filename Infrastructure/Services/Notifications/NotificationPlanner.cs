using System.Globalization;
using System.Text.Json;
using Core.DTO.Tenancy;
using Core.Entities.Notifications;
using Core.Enums.Notifications;
using Core.Events.Billing;
using Core.Events.Scheduling;
using Core.Interfaces.Clinic;
using Core.Interfaces.Common;
using Core.Interfaces.Patients;
using Core.Interfaces.Tenancy;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Notifications;

/// <summary>
/// Turns events into notification rows (ADR-015): what to send, to whom, when. Only adds/changes tracked rows of the
/// handler's context; the handler saves. The values are taken now (names, local date/time); the sender re-checks the
/// appointment before sending, so a later change cancels a stale message.
/// </summary>
public sealed class NotificationPlanner
{
    private readonly NotificationsDbContext _context;
    private readonly IPatientsApi _patients;
    private readonly IClinicApi _clinic;
    private readonly ITenancyApi _tenancy;
    private readonly ITenantContext _tenant;
    private readonly TimeProvider _time;
    private readonly NotificationOptions _options;

    public NotificationPlanner(NotificationsDbContext context, IPatientsApi patients, IClinicApi clinic, ITenancyApi tenancy, ITenantContext tenant,
        TimeProvider time, IOptions<NotificationOptions> options)
    {
        _context = context;
        _patients = patients;
        _clinic = clinic;
        _tenancy = tenancy;
        _tenant = tenant;
        _time = time;
        _options = options.Value;
    }

    /// <summary>An immediate message (confirmation, rescheduled, cancelled) and, if asked, the two reminders.</summary>
    public async Task PlanAppointmentAsync(AppointmentEvent e, NotificationKindEnum immediate, bool withReminders, CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (e.StartAt <= now || !await EnabledAsync(cancellationToken))
            return;

        var contact = await _patients.GetContactAsync(e.PatientId, cancellationToken);
        if (Recipient(contact) is not var (channel, recipient))
            return;

        var values = await AppointmentValuesAsync(e, contact!.FullName, cancellationToken);
        Add(immediate, channel, recipient, values, e.PatientId, e.BranchId, e.AppointmentId, e.StartAt, now);

        if (!withReminders)
            return;
        foreach (var (kind, before) in new[]
                 {
                     (NotificationKindEnum.AppointmentReminder24h, _options.FirstReminderBefore),
                     (NotificationKindEnum.AppointmentReminder2h, _options.SecondReminderBefore),
                 })
        {
            var at = e.StartAt - before;
            if (at >= now + _options.MinimumLeadTime)
                Add(kind, channel, recipient, values, e.PatientId, e.BranchId, e.AppointmentId, e.StartAt, at);
        }
    }

    /// <summary>Cancels the appointment's messages that haven't gone out yet.</summary>
    public async Task CancelScheduledAsync(long appointmentId, string reason, CancellationToken cancellationToken)
    {
        var pending = await _context.Notifications
            .Where(n => n.AppointmentId == appointmentId && n.Status == NotificationStatusEnum.Scheduled)
            .ToListAsync(cancellationToken);
        foreach (var notification in pending)
        {
            notification.Status = NotificationStatusEnum.Cancelled;
            notification.StatusReason = reason;
        }
    }

    public async Task PlanReceiptAsync(PaymentRecorded e, CancellationToken cancellationToken)
    {
        if (!await EnabledAsync(cancellationToken))
            return;

        var contact = await _patients.GetContactAsync(e.PatientId, cancellationToken);
        if (Recipient(contact) is not var (channel, recipient))
            return;

        var values = new Dictionary<string, string>
        {
            ["patient"] = FirstName(contact!.FullName),
            ["clinic"] = await ClinicNameAsync(cancellationToken),
            ["amount"] = e.Amount.ToString("0.00", CultureInfo.InvariantCulture),
            ["currency"] = e.CurrencyCode,
            ["invoice"] = e.InvoiceNumber,
            ["receipt"] = e.ReceiptNumber,
        };
        Add(NotificationKindEnum.PaymentReceipt, channel, recipient, values, e.PatientId, e.BranchId, appointmentId: null, appointmentStart: null,
            _time.GetUtcNow());
    }

    private void Add(NotificationKindEnum kind, NotificationChannelEnum channel, string recipient, Dictionary<string, string> values, long patientId,
        long branchId, long? appointmentId, DateTimeOffset? appointmentStart, DateTimeOffset scheduledAt) =>
        _context.Notifications.Add(new Notification
        {
            Kind = kind,
            Channel = channel,
            Recipient = recipient,
            Language = _options.Language,
            PayloadJson = JsonSerializer.Serialize(values),
            PatientId = patientId,
            BranchId = branchId,
            AppointmentId = appointmentId,
            AppointmentStartAt = appointmentStart,
            ScheduledAt = scheduledAt,
        });

    private Task<bool> EnabledAsync(CancellationToken cancellationToken) =>
        _tenancy.IsFeatureEnabledAsync(_tenant.TenantId ?? throw new InvalidOperationException("No tenant."), FeatureCodes.Notifications, cancellationToken);

    /// <summary>SMS to a phone when there is one, else e-mail; nothing when the patient has neither.</summary>
    private static (NotificationChannelEnum, string)? Recipient(Core.DTO.Patients.PatientContact? contact) =>
        contact?.PhoneNumber is { Length: > 0 } phone ? (NotificationChannelEnum.Sms, phone)
        : contact?.Email is { Length: > 0 } email ? (NotificationChannelEnum.Email, email)
        : null;

    private async Task<Dictionary<string, string>> AppointmentValuesAsync(AppointmentEvent e, string patientName, CancellationToken cancellationToken)
    {
        var doctors = await _clinic.GetDoctorNamesAsync([e.DoctorId], cancellationToken);
        var branches = await _clinic.GetBranchNamesAsync([e.BranchId], cancellationToken);
        var timeZoneId = await _clinic.GetBranchTimeZoneAsync(e.BranchId, cancellationToken);
        var timeZone = timeZoneId is null ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(e.StartAt, timeZone);

        return new Dictionary<string, string>
        {
            ["patient"] = FirstName(patientName),
            ["clinic"] = await ClinicNameAsync(cancellationToken),
            ["doctor"] = doctors.GetValueOrDefault(e.DoctorId, ""),
            ["branch"] = branches.GetValueOrDefault(e.BranchId, ""),
            ["date"] = local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["time"] = local.ToString("HH:mm", CultureInfo.InvariantCulture),
        };
    }

    private async Task<string> ClinicNameAsync(CancellationToken cancellationToken) =>
        (await _tenancy.GetTenantAsync(_tenant.TenantId!.Value, cancellationToken))?.Name ?? "";

    private static string FirstName(string fullName) => fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
}
