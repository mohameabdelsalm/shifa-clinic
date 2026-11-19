using Core.Enums.Scheduling;

namespace Infrastructure.Services.Scheduling;

public sealed record BookingRequest(long PatientId, long BranchId, long DoctorId, long ServiceId, DateTimeOffset StartAt, AppointmentSourceEnum Source);
