namespace Shifa.Scheduling.Enums;

public enum AppointmentStatusEnum
{
    Pending = 1,
    Confirmed,
    CheckedIn,
    InProgress,
    Completed,
    Cancelled,
    NoShow,
}

public enum AppointmentSourceEnum
{
    Online = 1,
    Reception,
    Phone,
}

/// <summary>Result of asking an appointment to move to a status.</summary>
public enum TransitionResult
{
    Changed,

    /// <summary>Already in the requested status: a repeated request is a harmless no-op.</summary>
    Unchanged,

    NotAllowed,
}
