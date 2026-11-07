using Core.Entities.Common;

namespace Core.Entities.Patients;

/// <summary>Contact details of a guardian (informational). Portal access for a guardian is a <see cref="PatientAccountAccess"/>.</summary>
public class PatientGuardian : TenantOwnedEntity
{
    public long PatientId { get; set; }
    public string FullName { get; set; } = "";
    public string Relationship { get; set; } = "";
    public string? PhoneNumber { get; set; }
}
