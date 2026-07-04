using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Persistence port for <see cref="Transaction"/> aggregates. Implemented in Infrastructure
/// over EF Core / PostgreSQL.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>Adds a new transaction and persists the change.</summary>
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the transaction with the given id, including its <see cref="Asset"/>, or
    /// <c>null</c> if none exists. Ownership scoping (FR-03) is enforced by the handler
    /// against the loaded transaction's <see cref="Transaction.PortfolioId"/>.
    /// </summary>
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing tracked transaction.</summary>
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>Removes a transaction and persists the change.</summary>
    Task RemoveAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a filtered, sorted, paged slice of a portfolio's transactions plus the total
    /// matching count (FR-07), each row including its <see cref="Asset"/> for projection.
    /// </summary>
    Task<PagedResult<Transaction>> ListByPortfolioAsync(
        TransactionQueryParameters parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Net held quantity of an asset within a portfolio: the sum of buy quantities minus
    /// sell quantities. <paramref name="excludeTransactionId"/> omits a single transaction
    /// from the sum — used when re-validating an edit so the row being changed doesn't
    /// count against itself. Backs the over-sell guard.
    /// </summary>
    Task<decimal> GetHeldQuantityAsync(
        Guid portfolioId,
        Guid assetId,
        Guid? excludeTransactionId = null,
        CancellationToken cancellationToken = default);
}
