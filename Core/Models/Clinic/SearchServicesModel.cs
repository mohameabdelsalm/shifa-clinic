using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Common;

namespace Core.Models.Clinic;

/// <summary>Search: English or Arabic name. SortBy: nameEn, nameAr, duration, status, date (default).</summary>
public class SearchServicesModel : PaginationParameters
{
    public long? SpecialtyId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public StatusEnum? Status { get; set; }
}
