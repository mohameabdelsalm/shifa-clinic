using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Billing;

namespace Core.Models.Billing;

/// <summary>Search: our reference or the gateway transaction id. SortBy: date (default), amount, status. Dates are UTC days of creation.</summary>
public class SearchOnlinePaymentsModel : PaginationParameters
{
    public long? InvoiceId { get; set; }
    public long? PatientId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OnlineAttemptStatusEnum? Status { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
