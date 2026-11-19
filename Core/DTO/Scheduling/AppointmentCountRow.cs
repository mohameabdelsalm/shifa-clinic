using Core.Enums.Scheduling;

namespace Core.DTO.Scheduling;

/// <summary>Number of appointments (and their total minutes) for one local day, branch, doctor and status (input for statistics).</summary>
public sealed record AppointmentCountRow(DateOnly LocalDate, long BranchId, long DoctorId, AppointmentStatusEnum Status, int Count, int Minutes = 0);
