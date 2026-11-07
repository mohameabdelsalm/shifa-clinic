namespace Core.Enums.Clinical;

public enum VisitStatusEnum
{
    Open = 1,

    /// <summary>Locked for ever; corrections are addenda (ADR-010).</summary>
    Completed,
}
