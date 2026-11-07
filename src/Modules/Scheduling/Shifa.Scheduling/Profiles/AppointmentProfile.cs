using Shifa.Scheduling.DTO;
using Shifa.Scheduling.Entities;
using Shifa.Scheduling.Services;

namespace Shifa.Scheduling.Profiles;

public static class AppointmentProfile
{
    public static IQueryable<AppointmentDto> ToDtos(this IQueryable<Appointment> appointments) =>
        appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            BranchId = a.BranchId,
            DoctorId = a.DoctorId,
            ServiceId = a.ServiceId,
            PatientId = a.PatientId,
            StartAt = a.StartAt,
            EndAt = a.EndAt,
            LocalDate = a.LocalDate,
            Status = a.Status.ToString(),
            Source = a.Source.ToString(),
            QueueNumber = a.QueueNumber,
            CheckedInAt = a.CheckedInAt,
            CancelledAt = a.CancelledAt,
            CancelReason = a.CancelReason,
        });

    public static List<AvailableSlotDto> ToDtos(this IEnumerable<AvailableSlot> slots) =>
        slots.Select(s => new AvailableSlotDto { StartAt = s.StartUtc, EndAt = s.EndUtc, LocalStart = s.LocalStart }).ToList();
}
