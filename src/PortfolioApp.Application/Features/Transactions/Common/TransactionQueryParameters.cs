using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Application.Features.Transactions.Common;

/// <summary>
/// Filter, sort, and paging inputs for a portfolio-scoped transaction list (FR-07). All
/// filters are optional; <see cref="PortfolioId"/> scopes the query to the caller's
/// portfolio (FR-03) and is resolved by the handler, not supplied by the client.
/// </summary>
public record TransactionQueryParameters(
    Guid PortfolioId,
    string? AssetTicker,
    TransactionType? Type,
    DateOnly? FromDate,
    DateOnly? ToDate,
    TransactionSortField SortBy,
    bool Descending,
    int Page,
    int PageSize);
