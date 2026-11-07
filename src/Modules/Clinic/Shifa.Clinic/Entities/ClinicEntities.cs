using Shifa.Core.Entities.Common;
using Shifa.Core.Enums.Common;

namespace Shifa.Clinic.Entities;

public class Specialty : TenantOwnedEntity
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public StatusEnum Status { get; set; } = StatusEnum.Active;
}

public class Doctor : TenantOwnedEntity
{
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Active;
}

public class DoctorSpecialty : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public long SpecialtyId { get; set; }
}

/// <summary>Where a doctor works. A doctor can work in several branches of the same tenant.</summary>
public class DoctorBranch : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
}

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

/// <summary>A period when a doctor doesn't work at any branch (leave, conference). Stored in UTC.</summary>
public class DoctorTimeOff : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}

public class Service : TenantOwnedEntity
{
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";

    /// <summary>How long the service takes; Scheduling rounds it up to whole slots.</summary>
    public int DurationMinutes { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}

/// <summary>
/// The price of a service at one branch. History is kept: changing a price closes the current row
/// (EffectiveTo) and adds a new one, so invoices can always show what was charged and when.
/// </summary>
public class ServicePrice : TenantOwnedEntity
{
    public long ServiceId { get; set; }
    public long BranchId { get; set; }
    public decimal Price { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}
