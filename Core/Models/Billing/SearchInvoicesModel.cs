using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Billing;

namespace Core.Models.Billing;

/// <summary>Search: invoice number. SortBy: issuedAt (default), number, total, balance, status. Dates are UTC days of IssuedAt.</summary>
public class SearchInvoicesModel : PaginationParameters
{
    public long? BranchId { get; set; }
    public long? PatientId { get; set; }
    public long? AppointmentId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InvoiceStatusEnum? Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InvoiceKindEnum? Kind { get; set; }

    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
