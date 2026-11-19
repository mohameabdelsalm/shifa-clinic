namespace Core.Events.Scheduling;

/// <summary>A Pending appointment was confirmed by reception.</summary>
public sealed record AppointmentConfirmed(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt)
    : AppointmentEvent(AppointmentId, PatientId, BranchId, DoctorId, ServiceId, StartAt);
