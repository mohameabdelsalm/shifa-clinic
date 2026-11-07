using System.Text.Json.Serialization;
using Shifa.Core.Common;

namespace Shifa.Clinic.Models;

public class AddBranchModel
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? TimeZoneId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class UpdateBranchModel
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? TimeZoneId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}

public class SearchBranchesModel : PaginationParameters
{
    public string? Text { get; set; }
}

public class AddSpecialtyModel
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
}

public class UpdateSpecialtyModel
{
    public long Id { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
}

public class AddDoctorModel
{
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public List<long> SpecialtyIds { get; set; } = [];
    public List<long> BranchIds { get; set; } = [];
}

public class UpdateDoctorModel
{
    public long Id { get; set; }
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public List<long> SpecialtyIds { get; set; } = [];

    /// <summary>The doctor's branches inside the caller's own branch scope; branches outside it are left as they are.</summary>
    public List<long> BranchIds { get; set; } = [];
}

public class SearchDoctorsModel : PaginationParameters
{
    public string? Text { get; set; }
    public long? SpecialtyId { get; set; }
    public long? BranchId { get; set; }
}

public class ScheduleItemModel
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotMinutes { get; set; }
}

/// <summary>Replaces the doctor's whole weekly schedule at one branch.</summary>
public class SetDoctorScheduleModel
{
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
    public List<ScheduleItemModel> Items { get; set; } = [];
}

public class AddTimeOffModel
{
    public long DoctorId { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}

public class AddServiceModel
{
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
}

public class UpdateServiceModel
{
    public long Id { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public int DurationMinutes { get; set; }
}

public class SearchServicesModel : PaginationParameters
{
    public string? Text { get; set; }
    public long? SpecialtyId { get; set; }
}

public class SetServicePriceModel
{
    public long ServiceId { get; set; }
    public long BranchId { get; set; }
    public decimal Price { get; set; }
}
