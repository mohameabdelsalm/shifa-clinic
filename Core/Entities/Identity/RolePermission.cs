using Core.Entities.Common;

namespace Core.Entities.Identity;

public class RolePermission : TenantOwnedEntity
{
    public long RoleId { get; set; }

    /// <summary>A code from <see cref="Core.Common.Permissions"/>.</summary>
    public string PermissionCode { get; set; } = "";
}
