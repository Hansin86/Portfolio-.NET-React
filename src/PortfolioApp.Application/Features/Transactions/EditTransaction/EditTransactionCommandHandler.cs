using AutoMapper;
using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Application.Features.Transactions.EditTransaction;

/// <summary>
/// Handles <see cref="EditTransactionCommand"/>: verifies the transaction belongs to the
/// caller (FR-03), re-validates the over-sell guard against the proposed change, applies the
/// edit, and returns the updated transaction.
/// </summary>
public class EditTransactionCommandHandler : IRequestHandler<EditTransactionCommand, TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPortfolioRepository _portfolios;
    private readonly ITransactionRepository _transactions;
    private readonly IMapper _mapper;

    public EditTransactionCommandHandler(
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

    public async Task<TransactionDto> Handle(EditTransactionCommand request, CancellationToken cancellationToken)
    {
        Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("No portfolio was found for the current user.");

        Transaction? transaction = await _transactions.GetByIdAsync(request.Id, cancellationToken);

        if (transaction is null || transaction.PortfolioId != portfolio.Id)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        // Re-validate holdings as if this transaction were replaced by its edited form: the
        // net position for the asset (everything except this row, plus the proposed change)
        // must not go negative.
        decimal heldExcludingThis = await _transactions.GetHeldQuantityAsync(
            portfolio.Id, transaction.AssetId, transaction.Id, cancellationToken);

        decimal proposedContribution = request.Type == TransactionType.Buy
            ? request.Quantity
            : -request.Quantity;

        if (heldExcludingThis + proposedContribution < 0)
        {
            throw new DomainException(
                $"This change would leave a negative holding of {transaction.Asset.Ticker}.");
        }

        transaction.Type = request.Type;
        transaction.Quantity = request.Quantity;
        transaction.PricePerUnit = request.PricePerUnit;
        transaction.Currency = Currency.From(request.Currency);
        transaction.TransactionDate = request.TransactionDate;

        await _transactions.UpdateAsync(transaction, cancellationToken);

        return _mapper.Map<TransactionDto>(transaction);
    }
}
