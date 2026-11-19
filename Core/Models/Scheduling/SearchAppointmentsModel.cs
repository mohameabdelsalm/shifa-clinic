using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Scheduling;

namespace Core.Models.Scheduling;

/// <summary>SortBy: startAt (default), status, branch, doctor, queueNumber, date. Search isn't used (no text columns).</summary>
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
