using Core.Entities.Common;
using Core.Enums.Patients;

namespace Core.Entities.Patients;

public class PatientConsent : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public ConsentTypeEnum ConsentType { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null;
}
