using Core.Enums.Scheduling;

namespace Core.DTO.Scheduling;

/// <summary>What Clinical needs from an appointment to open a visit for it.</summary>
public sealed record AppointmentForVisit(long Id, long PatientId, long DoctorId, long BranchId, AppointmentStatusEnum Status, long ServiceId = 0);
