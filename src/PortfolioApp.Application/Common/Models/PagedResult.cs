namespace PortfolioApp.Application.Common.Models;

/// <summary>
/// A single page of results plus the total number of matching records, so a client can
/// render pagination controls without a separate count request.
/// </summary>
/// <param name="Items">The records on the requested page.</param>
/// <param name="TotalCount">Total number of records matching the query, across all pages.</param>
/// <param name="Page">1-based page number these <paramref name="Items"/> belong to.</param>
/// <param name="PageSize">Maximum number of records per page.</param>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
