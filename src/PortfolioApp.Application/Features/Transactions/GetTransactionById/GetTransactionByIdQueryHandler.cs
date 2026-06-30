using AutoMapper;
using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Application.Features.Transactions.GetTransactionById;

/// <summary>
/// Handles <see cref="GetTransactionByIdQuery"/>: loads the transaction and verifies it
/// belongs to the caller's portfolio, throwing <see cref="NotFoundException"/> otherwise so
/// existence isn't leaked across users (FR-03).
/// </summary>
public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPortfolioRepository _portfolios;
    private readonly ITransactionRepository _transactions;
    private readonly IMapper _mapper;

    public GetTransactionByIdQueryHandler(
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

    public async Task<TransactionDto> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("No portfolio was found for the current user.");

        Transaction? transaction = await _transactions.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null || transaction.PortfolioId != portfolio.Id)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        return _mapper.Map<TransactionDto>(transaction);
    }
}
