namespace Core.DTO.Clinic;

public class DoctorDto
{
    public long Id { get; set; }
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public string Status { get; set; } = "";

    /// <summary>The linked staff account, if any.</summary>
    public long? MembershipId { get; set; }
    public List<long> SpecialtyIds { get; set; } = [];

    /// <summary>Only the branches the caller may see.</summary>
    public List<long> BranchIds { get; set; } = [];
    public List<DoctorScheduleDto> Schedules { get; set; } = [];
    public List<DoctorTimeOffDto> TimeOffs { get; set; } = [];
}
