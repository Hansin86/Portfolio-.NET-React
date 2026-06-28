using MediatR;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Application.Features.Transactions.DeleteTransaction;

/// <summary>
/// Handles <see cref="DeleteTransactionCommand"/>: verifies the transaction belongs to the
/// caller (FR-03) and removes it.
/// </summary>
public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPortfolioRepository _portfolios;
    private readonly ITransactionRepository _transactions;

    public DeleteTransactionCommandHandler(
        ICurrentUserService currentUser,
        IPortfolioRepository portfolios,
        ITransactionRepository transactions)
    {
        _currentUser = currentUser;
        _portfolios = portfolios;
        _transactions = transactions;
    }

    public async Task Handle(DeleteTransactionCommand request, CancellationToken cancellationToken)
    {
        Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("No portfolio was found for the current user.");

        Transaction? transaction = await _transactions.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null || transaction.PortfolioId != portfolio.Id)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        await _transactions.RemoveAsync(transaction, cancellationToken);
    }
}
