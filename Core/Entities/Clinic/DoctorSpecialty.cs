using Core.Entities.Common;

namespace Core.Entities.Clinic;

public class DoctorSpecialty : TenantOwnedEntity
{
    public long DoctorId { get; set; }
    public long SpecialtyId { get; set; }
}
