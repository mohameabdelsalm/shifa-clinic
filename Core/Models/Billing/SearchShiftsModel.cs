using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Billing;

namespace Core.Models.Billing;

/// <summary>SortBy: openedAt (default), closedAt, variance, status. Dates are UTC days of OpenedAt.</summary>
public class SearchShiftsModel : PaginationParameters
{
    public long? BranchId { get; set; }
    public long? CashierUserId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShiftStatusEnum? Status { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
