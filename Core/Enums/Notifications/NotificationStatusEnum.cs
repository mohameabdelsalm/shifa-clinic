namespace Core.Enums.Notifications;

public enum NotificationStatusEnum
{
    /// <summary>Waiting for its time (ScheduledAt).</summary>
    Scheduled = 1,
    Sent,

    /// <summary>The provider failed 3 times.</summary>
    Failed,

    /// <summary>No longer valid: the appointment was cancelled, moved or attended.</summary>
    Cancelled,

    /// <summary>Not sent on purpose: feature off, clinic not active, or too late to be useful.</summary>
    Skipped,
}
