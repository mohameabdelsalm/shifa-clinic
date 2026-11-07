namespace Shifa.Scheduling.Contracts.Events;

// Integration events of the Scheduling module (ADR-005, ADR-008). Defined now so consumers can be designed;
// they are written to the outbox and published in Phase 8. Every event carries its TenantId.

public sealed record AppointmentBooked(Guid EventId, long TenantId, long AppointmentId, long BranchId, long DoctorId,
    long ServiceId, long PatientId, DateTimeOffset StartAt, DateTimeOffset EndAt, string Source);

public sealed record AppointmentCancelled(Guid EventId, long TenantId, long AppointmentId, long PatientId, DateTimeOffset StartAt);

public sealed record PatientCheckedIn(Guid EventId, long TenantId, long AppointmentId, long BranchId, long DoctorId,
    long ServiceId, long PatientId, DateTimeOffset CheckedInAt);

public sealed record AppointmentNoShow(Guid EventId, long TenantId, long AppointmentId, long PatientId);
