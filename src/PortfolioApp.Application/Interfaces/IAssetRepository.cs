using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Persistence port for <see cref="Asset"/> aggregates. Assets are shared across all
/// portfolios; transactions reference them by ticker via a get-or-create flow. Implemented
/// in Infrastructure over EF Core / PostgreSQL.
/// </summary>
public interface IAssetRepository
{
    /// <summary>
    /// Returns the asset with the given ticker, or <c>null</c> if none exists. Ticker
    /// matching is case-insensitive (tickers are stored upper-cased).
    /// </summary>
    Task<Asset?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default);

    /// <summary>Adds a new asset and persists the change.</summary>
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);
}
