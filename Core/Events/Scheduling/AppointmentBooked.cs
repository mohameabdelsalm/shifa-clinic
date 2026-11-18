namespace Core.Events.Scheduling;

/// <param name="Confirmed">False when the booking waits for reception (Pending, e.g. after 3 no-shows).</param>
public sealed record AppointmentBooked(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt, bool Confirmed)
    : AppointmentEvent(AppointmentId, PatientId, BranchId, DoctorId, ServiceId, StartAt);
