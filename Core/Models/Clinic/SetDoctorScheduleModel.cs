namespace Core.Models.Clinic;

/// <summary>Replaces the doctor's whole weekly schedule at one branch.</summary>
public class SetDoctorScheduleModel
{
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
    public List<ScheduleItemModel> Items { get; set; } = [];
}
