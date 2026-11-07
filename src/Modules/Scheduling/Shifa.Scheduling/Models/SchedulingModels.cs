using System.Text.Json.Serialization;
using Shifa.Core.Common;
using Shifa.Scheduling.Enums;

namespace Shifa.Scheduling.Models;

public class AvailabilityModel
{
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }

    /// <summary>The day in the branch's local calendar.</summary>
    public DateOnly Date { get; set; }
}

public class BookAppointmentModel
{
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }

    /// <summary>One of the starts returned by the availability endpoint (any offset; stored as UTC).</summary>
    public DateTimeOffset StartAt { get; set; }

    /// <summary>Reception or Phone for staff bookings.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppointmentSourceEnum Source { get; set; } = AppointmentSourceEnum.Reception;
}

public class PortalBookAppointmentModel
{
    public long PatientId { get; set; }
    public long BranchId { get; set; }
    public long DoctorId { get; set; }
    public long ServiceId { get; set; }
    public DateTimeOffset StartAt { get; set; }
}

public class RescheduleAppointmentModel
{
    public long Id { get; set; }
    public DateTimeOffset StartAt { get; set; }
}

public class CancelAppointmentModel
{
    public string? Reason { get; set; }
}

public class SearchAppointmentsModel : PaginationParameters
{
    /// <summary>Branch-local dates, inclusive; at most 62 days apart.</summary>
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? BranchId { get; set; }
    public long? DoctorId { get; set; }
    public long? PatientId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AppointmentStatusEnum? Status { get; set; }
}
