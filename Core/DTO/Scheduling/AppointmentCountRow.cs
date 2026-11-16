using Core.Enums.Scheduling;

namespace Core.DTO.Scheduling;

/// <summary>Number of appointments for one local day, branch, doctor and status (input for statistics).</summary>
public sealed record AppointmentCountRow(DateOnly LocalDate, long BranchId, long DoctorId, AppointmentStatusEnum Status, int Count);
