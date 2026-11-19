using System.Text.Json.Serialization;
using Core.Enums.Scheduling;

namespace Core.Models.Scheduling;

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
