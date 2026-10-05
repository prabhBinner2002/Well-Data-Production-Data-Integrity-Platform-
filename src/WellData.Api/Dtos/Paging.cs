using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WellData.Api.Dtos;

/// <summary>
/// Offset paging parameters shared by every list endpoint.
/// Controller parameters of this type must not be named "page": MVC would treat the "page" query key as
/// the prefix for the whole object and then find no "page.Page" or "page.PageSize" to bind.
/// </summary>
public sealed class PageQuery
{
    public const int MaxPageSize = 200;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 50;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class PagingExtensions
{
    /// <summary>Counts the full result, then fetches one page. The query must already be ordered.</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page.Page - 1) * page.PageSize)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page.Page, page.PageSize, totalCount);
    }
}
