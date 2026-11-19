using Core.Common;
using Core.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public static class PagedListExtensions
{
    /// <summary>Counts, then reads one page. The query must already be ordered.</summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> query, PaginationParameters paging, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(paging.PageNumber, 1);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pageNumber - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(cancellationToken);
        return new PagedList<T>(items, total, pageNumber, paging.PageSize);
    }
}
