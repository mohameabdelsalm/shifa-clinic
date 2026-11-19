namespace Core.Events.Scheduling;

/// <summary>Common shape of appointment events: who, where, when, as they were at the time of the change.</summary>
public abstract record AppointmentEvent(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt)
    : IIntegrationEvent;
