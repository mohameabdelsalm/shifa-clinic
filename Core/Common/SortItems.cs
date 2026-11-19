using System.Linq.Expressions;
using Core.Enums.Common;

namespace Core.Common;

/// <summary>
/// Sorting helper for the Sort... methods of the repos (Trio HRM pattern). Each repo maps the allowed
/// <see cref="PaginationParameters.SortBy"/> names to columns in a switch, so clients can only sort by known columns.
/// </summary>
public static class SortItems
{
    /// <summary>Orders by <paramref name="key"/>: ascending only when asked, otherwise descending (as in HRM).</summary>
    public static IOrderedQueryable<TEntity> OrderBy<TEntity, TKey>(this IQueryable<TEntity> query, OrderTypeEnum? orderType,
        Expression<Func<TEntity, TKey>> key) =>
        orderType == OrderTypeEnum.Ascending ? query.OrderBy(key) : query.OrderByDescending(key);
}
