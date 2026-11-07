using Core.Entities.Common;
using Core.Enums.Common;

namespace Core.Entities.Clinic;

public class Service : TenantOwnedEntity
{
    public long SpecialtyId { get; set; }
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";

    /// <summary>How long the service takes; Scheduling rounds it up to whole slots.</summary>
    public int DurationMinutes { get; set; }

    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
