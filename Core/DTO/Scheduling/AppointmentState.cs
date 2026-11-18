using Core.Enums.Scheduling;

namespace Core.DTO.Scheduling;

/// <summary>The current status and time of an appointment (checked before sending a reminder).</summary>
public sealed record AppointmentState(long Id, AppointmentStatusEnum Status, DateTimeOffset StartAt);
