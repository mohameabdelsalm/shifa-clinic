using Core.Entities.Common;
using Core.Enums.Patients;

namespace Core.Entities.Patients;

/// <summary>
/// Lets whoever proves ownership of <see cref="PhoneNumber"/> (OTP) act for the patient in the portal.
/// One parent's phone can be linked to several children. Linked by phone number, not by user id, so a
/// profile can be prepared before the person ever signs in and Identity data stays separate from medical data.
/// </summary>
public class PatientAccountAccess : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string PhoneNumber { get; set; } = "";
    public RelationshipEnum Relationship { get; set; }
    public bool CanBook { get; set; } = true;
}
