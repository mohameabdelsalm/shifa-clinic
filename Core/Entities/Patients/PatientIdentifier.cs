using Core.Entities.Common;
using Core.Enums.Patients;

namespace Core.Entities.Patients;

public class PatientIdentifier : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public IdentifierTypeEnum Type { get; set; }

    /// <summary>National ID or passport number. Sensitive: never logged.</summary>
    public string Value { get; set; } = "";
}
