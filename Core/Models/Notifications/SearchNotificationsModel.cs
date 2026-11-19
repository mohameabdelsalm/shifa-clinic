using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Notifications;

namespace Core.Models.Notifications;

/// <summary>SortBy: scheduledAt (default), status, kind. Dates are UTC days of ScheduledAt.</summary>
public class SearchNotificationsModel : PaginationParameters
{
    public long? PatientId { get; set; }
    public long? AppointmentId { get; set; }
    public long? BranchId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationStatusEnum? Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NotificationKindEnum? Kind { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
