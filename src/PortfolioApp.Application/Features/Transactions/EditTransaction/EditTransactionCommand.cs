using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Application.Features.Transactions.EditTransaction;

/// <summary>
/// Updates an existing transaction owned by the caller (FR-06). The asset (ticker) is not
/// editable — correcting a wrong ticker means deleting and re-adding — so only the trade
/// details below change.
/// </summary>
public record EditTransactionCommand(
    Guid Id,
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    string Currency,
    DateOnly TransactionDate) : IRequest<TransactionDto>;
