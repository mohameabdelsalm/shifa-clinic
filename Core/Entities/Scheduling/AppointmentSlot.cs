using Core.Entities.Common;

namespace Core.Entities.Scheduling;

/// <summary>
/// One 5-minute tick of a doctor's time claimed by an appointment. The unique key (TenantId, DoctorId, SlotStart)
/// over live rows is the final guard against double booking, at any branch (ADR-008).
/// </summary>
public class AppointmentSlot : TenantOwnedEntity
{
    public long AppointmentId { get; set; }
    public long DoctorId { get; set; }
    public DateTimeOffset SlotStart { get; set; }
}
