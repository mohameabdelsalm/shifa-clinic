using Core.Entities.Common;

namespace Core.Entities.Identity;

/// <summary>Append-only audit trail. TenantId is the tenant of the actor, TargetTenantId the tenant acted upon (Platform Admin actions).</summary>
[PlatformOwned]
public class AuditLog : BaseModel
{
    public long? UserId { get; set; }
    public long? TenantId { get; set; }
    public long? TargetTenantId { get; set; }
    public string Action { get; set; } = "";
    public string? Entity { get; set; }
    public long? EntityId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
}
