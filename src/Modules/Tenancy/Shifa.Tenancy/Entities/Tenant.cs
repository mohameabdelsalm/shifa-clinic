using Shifa.Core.Entities.Common;
using Shifa.Tenancy.Enums;

namespace Shifa.Tenancy.Entities;

/// <summary>
/// A customer organization. Platform-owned: tenants are managed by the Platform Admin,
/// so this table has no TenantId (its Id *is* the tenant id used everywhere else).
/// </summary>
[PlatformOwned]
public class Tenant : BaseModel
{
    public string Name { get; set; } = "";

    /// <summary>URL-friendly unique name, used to pick the clinic at sign-in and in the patient portal.</summary>
    public string Slug { get; set; } = "";

    public TenantStatusEnum Status { get; set; } = TenantStatusEnum.Trial;

    /// <summary>IANA time zone used when a branch doesn't set its own.</summary>
    public string DefaultTimeZoneId { get; set; } = "Africa/Cairo";

    public string CurrencyCode { get; set; } = "EGP";

    public bool IsUsable => Status is TenantStatusEnum.Trial or TenantStatusEnum.Active;

    /// <summary>Returns false when the tenant is closed (closing is final).</summary>
    public bool TrySuspend()
    {
        if (Status == TenantStatusEnum.Closed)
            return false;
        Status = TenantStatusEnum.Suspended;
        return true;
    }

    /// <summary>Back to Active. Returns false when the tenant is closed.</summary>
    public bool TryActivate()
    {
        if (Status == TenantStatusEnum.Closed)
            return false;
        Status = TenantStatusEnum.Active;
        return true;
    }
}
