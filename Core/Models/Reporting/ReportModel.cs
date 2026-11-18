using System.Text.Json.Serialization;
using Core.Enums.Reporting;

namespace Core.Models.Reporting;

/// <summary>Period, filters and output format of a report. Dates are branch-local days, inclusive; at most 366 days.</summary>
public class ReportModel
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    /// <summary>Limit to these branches (always within the caller's branch scope). Empty = every branch in scope.</summary>
    public List<long>? BranchIds { get; set; }

    public long? DoctorId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ReportFormatEnum Format { get; set; } = ReportFormatEnum.Json;
}
