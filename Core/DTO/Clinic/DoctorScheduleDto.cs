namespace Core.DTO.Clinic;

public class DoctorScheduleDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public string DayOfWeek { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}
