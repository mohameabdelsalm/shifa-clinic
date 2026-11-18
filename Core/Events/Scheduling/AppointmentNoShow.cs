namespace Core.Events.Scheduling;

/// <param name="Automatic">True when the no-show job marked it (30 minutes after the start, A10).</param>
public sealed record AppointmentNoShow(long AppointmentId, long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt, bool Automatic)
    : AppointmentEvent(AppointmentId, PatientId, BranchId, DoctorId, ServiceId, StartAt);
