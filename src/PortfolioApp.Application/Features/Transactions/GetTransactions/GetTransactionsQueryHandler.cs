using AutoMapper;
using MediatR;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Application.Features.Transactions.GetTransactions;

/// <summary>
/// Handles <see cref="GetTransactionsQuery"/>: resolves the caller's portfolio (FR-03) and
/// returns a filtered, sorted, paged slice of its transactions (FR-07).
/// </summary>
public class GetTransactionsQueryHandler
    : IRequestHandler<GetTransactionsQuery, PagedResult<TransactionDto>>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPortfolioRepository _portfolios;
    private readonly ITransactionRepository _transactions;
    private readonly IMapper _mapper;

    public GetTransactionsQueryHandler(
        ICurrentUserService currentUser,
        IPortfolioRepository portfolios,
        ITransactionRepository transactions,
        IMapper mapper)
    {
        _currentUser = currentUser;
        _portfolios = portfolios;
        _transactions = transactions;
        _mapper = mapper;
    }

    public async Task<PagedResult<TransactionDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("No portfolio was found for the current user.");

        var parameters = new TransactionQueryParameters(
            portfolio.Id,
            request.AssetTicker,
            request.Type,
            request.FromDate,
            request.ToDate,
            request.SortBy,
            request.Descending,
            request.Page,
            request.PageSize);

        PagedResult<Transaction> page = await _transactions.ListByPortfolioAsync(parameters, cancellationToken);

        IReadOnlyList<TransactionDto> items = _mapper.Map<IReadOnlyList<TransactionDto>>(page.Items);

        return new PagedResult<TransactionDto>(items, page.TotalCount, page.Page, page.PageSize);
    }
}
