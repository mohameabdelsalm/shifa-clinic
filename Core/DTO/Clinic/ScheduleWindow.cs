namespace Core.DTO.Clinic;

/// <summary>A weekly working window of a doctor at one branch, in the branch's local time.</summary>
public sealed record ScheduleWindow(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, int SlotMinutes);
