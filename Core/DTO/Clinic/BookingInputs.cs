namespace Core.DTO.Clinic;

/// <summary>Everything Scheduling needs to compute availability and validate a booking (ADR-008).</summary>
public sealed record BookingInputs(
    long BranchId,
    string TimeZoneId,
    long DoctorId,
    long ServiceId,
    int ServiceDurationMinutes,
    IReadOnlyList<ScheduleWindow> Schedule);
