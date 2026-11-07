using Core.Entities.Common;
using Core.Enums.Common;

namespace Core.Entities.Clinic;

public class Doctor : TenantOwnedEntity
{
    public string FullNameEn { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string? Title { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Active;

    /// <summary>The staff account (Identity membership) of this doctor; needed to open and write visits (ADR-010).</summary>
    public long? MembershipId { get; set; }
}
