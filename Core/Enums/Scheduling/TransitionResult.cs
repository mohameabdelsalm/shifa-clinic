namespace Core.Enums.Scheduling;

/// <summary>Result of asking an appointment to move to a status.</summary>
public enum TransitionResult
{
    Changed,

    /// <summary>Already in the requested status: a repeated request is a harmless no-op.</summary>
    Unchanged,

    NotAllowed,
}
