using System.Text.Json.Serialization;
using Core.Common;
using Core.Enums.Tenancy;

namespace Core.Models.Tenancy;

/// <summary>Search: name or slug. SortBy: name, slug, status, date (default).</summary>
public class SearchTenantsModel : PaginationParameters
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TenantStatusEnum? Status { get; set; }
}
