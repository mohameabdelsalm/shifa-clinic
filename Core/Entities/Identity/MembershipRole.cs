using Core.Entities.Common;

namespace Core.Entities.Identity;

public class MembershipRole : TenantOwnedEntity
{
    public long MembershipId { get; set; }
    public long RoleId { get; set; }
}
