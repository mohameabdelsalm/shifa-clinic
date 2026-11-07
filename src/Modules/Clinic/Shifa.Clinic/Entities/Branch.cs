using Shifa.Core.Entities.Common;
using Shifa.Core.Enums.Common;

namespace Shifa.Clinic.Entities;

/// <summary>
/// A physical clinic of a tenant. Tenant-owned: TenantId is stamped automatically on insert.
/// Full branch management (endpoints, rules, limits per plan) comes in Phase 3.
/// </summary>
public class Branch : TenantOwnedEntity
{
    public string Name { get; set; } = "";

    /// <summary>Short code, unique inside the tenant (e.g. "NASR-CITY").</summary>
    public string Code { get; set; } = "";

    /// <summary>IANA time zone used to calculate schedules and show times.</summary>
    public string TimeZoneId { get; set; } = "Africa/Cairo";

    public string? Phone { get; set; }
    public string? Address { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
