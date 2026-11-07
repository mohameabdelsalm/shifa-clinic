using Core.Pagination;

namespace Core.Common;

/// <summary>Base of every Search...Model. Page size is capped so one request can't read a whole table.</summary>
public class PaginationParameters
{
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, PagedList<object>.MaxPageSize);
    }
}
