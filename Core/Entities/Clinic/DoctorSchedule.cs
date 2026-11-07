using Core.Entities.Common;

namespace Core.Entities.Clinic;

/// <summary>Weekly working hours of a doctor at one branch. Times are local to the branch's time zone.</summary>
public class DoctorSchedule : TenantOwnedEntity
{
    public long DoctorBranchId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Length of one bookable slot; the window length must be a whole number of slots.</summary>
    public int SlotMinutes { get; set; }
}
