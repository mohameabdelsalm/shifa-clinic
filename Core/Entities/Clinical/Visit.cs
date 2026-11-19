using Core.Entities.Common;
using Core.Enums.Clinical;

namespace Core.Entities.Clinical;

/// <summary>
/// The doctor's encounter with the patient for one appointment. Open while the doctor works on it; once Completed it is
/// read-only and corrections are <see cref="ClinicalAddendum"/> rows (business rule 7, ADR-010).
/// Appointment, patient, doctor and branch are ids of other modules (no cross-schema keys).
/// </summary>
public class Visit : TenantOwnedEntity
{
    public long AppointmentId { get; set; }
    public long PatientId { get; set; }
    public long DoctorId { get; set; }
    public long BranchId { get; set; }
    public VisitStatusEnum Status { get; private set; } = VisitStatusEnum.Open;
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Every change to the visit or its content updates this row, so a write racing "complete" fails (409).</summary>
    public byte[] RowVersion { get; set; } = [];

    public bool IsOpen => Status == VisitStatusEnum.Open;

    /// <summary>Locks the visit. Returns false when it was already completed.</summary>
    public bool Complete(DateTimeOffset now)
    {
        if (!IsOpen)
            return false;

        Status = VisitStatusEnum.Completed;
        CompletedAt = now;
        return true;
    }
}
