using MediatR;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Application.Features.Transactions.GetTransactions;

/// <summary>
/// Lists the caller's transactions with optional filtering, sorting, and paging (FR-07).
/// Results are always scoped to the caller's own portfolio (FR-03); the portfolio is
/// resolved by the handler, never supplied by the client.
/// </summary>
public record GetTransactionsQuery(
    string? AssetTicker = null,
    TransactionType? Type = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    TransactionSortField SortBy = TransactionSortField.TransactionDate,
    bool Descending = true,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TransactionDto>>;
