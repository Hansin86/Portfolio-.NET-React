using MediatR;

namespace PortfolioApp.Application.Features.Transactions.DeleteTransaction;

/// <summary>
/// Deletes a transaction owned by the caller (FR-06). A transaction that does not exist or
/// belongs to another user surfaces identically as a
/// <see cref="Domain.Exceptions.NotFoundException"/> (404).
/// </summary>
public record DeleteTransactionCommand(Guid Id) : IRequest;
