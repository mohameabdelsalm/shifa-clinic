using Core.Entities.Scheduling;
using Core.Enums.Scheduling;
using Core.Events.Scheduling;

namespace Infrastructure.Services.Scheduling;

/// <summary>Builds the appointment integration events (ADR-015) from the appointment as it is being saved.</summary>
public static class AppointmentEvents
{
    public static AppointmentBooked Booked(Appointment a) =>
        new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt, a.Status == AppointmentStatusEnum.Confirmed);

    public static AppointmentConfirmed Confirmed(Appointment a) => new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt);

    public static AppointmentRescheduled Rescheduled(Appointment a, DateTimeOffset previousStart) =>
        new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt, previousStart, a.Status == AppointmentStatusEnum.Confirmed);

    public static AppointmentCancelled Cancelled(Appointment a) => new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt);

    public static PatientCheckedIn CheckedIn(Appointment a) => new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt, a.QueueNumber);

    public static AppointmentNoShow NoShow(Appointment a, bool automatic) =>
        new(a.Id, a.PatientId, a.BranchId, a.DoctorId, a.ServiceId, a.StartAt, automatic);
}
