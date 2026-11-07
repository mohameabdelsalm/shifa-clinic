namespace Shifa.Clinic.DTO;

public class BranchDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string TimeZoneId { get; set; } = "";
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = "";
}

public class SpecialtyDto
{
    public long Id { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string Status { get; set; } = "";
}

public class DoctorScheduleDto
{
    public long Id { get; set; }
    public long BranchId { get; set; }
    public string DayOfWeek { get; set; } = "";
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}

public class DoctorTimeOffDto
{
    public long Id { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}

public class DoctorDto
{
    public long Id { get; set; }
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public string Status { get; set; } = "";
    public List<long> SpecialtyIds { get; set; } = [];

    /// <summary>Only the branches the caller may see.</summary>
    public List<long> BranchIds { get; set; } = [];
    public List<DoctorScheduleDto> Schedules { get; set; } = [];
    public List<DoctorTimeOffDto> TimeOffs { get; set; } = [];
}

public class ServicePriceDto
{
    public long BranchId { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
}

public class ServiceDto
{
    public long Id { get; set; }
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "";

    /// <summary>Current prices, only for the branches the caller may see.</summary>
    public List<ServicePriceDto> Prices { get; set; } = [];
}
