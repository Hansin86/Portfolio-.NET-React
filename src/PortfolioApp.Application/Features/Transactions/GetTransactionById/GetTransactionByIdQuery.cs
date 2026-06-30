using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;

namespace PortfolioApp.Application.Features.Transactions.GetTransactionById;

/// <summary>
/// Fetches a single transaction by id, scoped to the caller's portfolio (FR-03). A
/// transaction that does not exist or belongs to another user surfaces identically as a
/// <see cref="Domain.Exceptions.NotFoundException"/> (404).
/// </summary>
public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDto>;
