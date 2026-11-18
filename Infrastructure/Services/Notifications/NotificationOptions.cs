namespace Infrastructure.Services.Notifications;

/// <summary>The "Notifications" configuration section.</summary>
public sealed class NotificationOptions
{
    public const string Section = "Notifications";

    /// <summary>Language of messages to patients (no per-patient language yet): "ar" or "en".</summary>
    public string Language { get; set; } = "ar";

    /// <summary>Reminders before the appointment start.</summary>
    public TimeSpan FirstReminderBefore { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan SecondReminderBefore { get; set; } = TimeSpan.FromHours(2);

    /// <summary>A reminder that would go out sooner than this after booking is not scheduled.</summary>
    public TimeSpan MinimumLeadTime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Wait before retrying a failed send.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
}
