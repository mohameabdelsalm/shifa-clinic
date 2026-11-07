using Core.Entities.Common;

namespace Core.Entities.Clinic;

/// <summary>A period when a doctor doesn't work at any branch (leave, conference). Stored in UTC.</summary>
public class DoctorTimeOff : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public DateTimeOffset FromAt { get; set; }
    public DateTimeOffset ToAt { get; set; }
    public string? Reason { get; set; }
}
