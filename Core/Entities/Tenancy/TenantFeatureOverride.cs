using Core.Entities.Common;

namespace Core.Entities.Tenancy;

/// <summary>A feature switched on or off for one tenant by the Platform Admin, whatever its plan says.</summary>
[PlatformOwned]
public class TenantFeatureOverride : BaseModel
{
    public long TenantId { get; set; }
    public string Code { get; set; } = "";
    public bool Enabled { get; set; }
}
