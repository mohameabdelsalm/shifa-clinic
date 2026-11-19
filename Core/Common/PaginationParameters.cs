using System.Text.Json.Serialization;
using Core.Enums.Common;
using Core.Pagination;

namespace Core.Common;

/// <summary>
/// Base of every Search...Model, like Trio HRM: paging, sorting and a free-text search.
/// Page size is capped so one request can't read a whole table.
/// </summary>
public class PaginationParameters
{
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, PagedList<object>.MaxPageSize);
    }

    /// <summary>Column to sort by (each search lists the names it accepts; anything else uses its default order).</summary>
    public string? SortBy { get; set; }

    /// <summary>Ascending or Descending (default Descending, like HRM).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderTypeEnum? SortType { get; set; }

    /// <summary>Free text, matched against the main text columns of each search (e.g. name, code, MRN).</summary>
    public string? Search { get; set; }

    /// <summary>The trimmed search text, or null when empty.</summary>
    [JsonIgnore]
    public string? SearchText => string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
}
