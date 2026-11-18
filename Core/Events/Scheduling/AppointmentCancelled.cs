namespace Core.Events.Scheduling;

public sealed record AppointmentCancelled(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt)
    : AppointmentEvent(AppointmentId, PatientId, BranchId, DoctorId, ServiceId, StartAt);
