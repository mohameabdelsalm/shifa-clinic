using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Common;

namespace Core.Models.Clinic;

/// <summary>Search: English or Arabic name. SortBy: nameEn, nameAr, status, date (default).</summary>
public class SearchDoctorsModel : PaginationParameters
{
    public long? SpecialtyId { get; set; }
    public long? BranchId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum? Status { get; set; }
}
