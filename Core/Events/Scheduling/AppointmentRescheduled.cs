namespace Core.Events.Scheduling;

public sealed record AppointmentRescheduled(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt,
    DateTimeOffset PreviousStartAt, bool Confirmed)
    : AppointmentEvent(AppointmentId, PatientId, BranchId, DoctorId, ServiceId, StartAt);
