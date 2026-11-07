using Core.Entities.Common;
using Core.Enums.Scheduling;

namespace Core.Entities.Scheduling;

public class AppointmentStatusHistory : TenantOwnedEntity
{
    public long AppointmentId { get; set; }
    public AppointmentStatusEnum? FromStatus { get; set; }
    public AppointmentStatusEnum ToStatus { get; set; }
    public string? Reason { get; set; }
}
