using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Common;

namespace Core.Models.Clinic;

/// <summary>Search: name or code. SortBy: name, code, status, date (default).</summary>
public class SearchBranchesModel : PaginationParameters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum? Status { get; set; }
}
