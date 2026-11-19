using Core.Entities.Common;
using Core.Enums.Common;

namespace Core.Entities.Clinic;

public class Specialty : TenantOwnedEntity
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public StatusEnum Status { get; set; } = StatusEnum.Active;
}
