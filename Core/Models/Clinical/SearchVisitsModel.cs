using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Clinical;

namespace Core.Models.Clinical;

public class SearchVisitsModel : PaginationParameters
{
    public long? PatientId { get; set; }
    public long? DoctorId { get; set; }
    public long? BranchId { get; set; }

    /// <summary>Opened on or after this UTC date.</summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>Opened on or before this UTC date.</summary>
    public DateOnly? ToDate { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VisitStatusEnum? Status { get; set; }
}
