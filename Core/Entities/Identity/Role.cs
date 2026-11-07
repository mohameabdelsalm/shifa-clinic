using Core.Entities.Common;

namespace Core.Entities.Identity;

public class Role : TenantOwnedEntity
{
    public string Name { get; set; } = "";

    /// <summary>Created at onboarding. System roles can't be deleted, and TenantAdmin can't be edited.</summary>
    public bool IsSystemDefault { get; set; }
}
