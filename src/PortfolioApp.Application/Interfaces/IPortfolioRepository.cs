using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Persistence port for <see cref="Portfolio"/> aggregates. Implemented in Infrastructure
/// over EF Core / PostgreSQL.
/// </summary>
public interface IPortfolioRepository
{
    /// <summary>
    /// Returns the portfolio owned by the given user, or <c>null</c> if none exists. Each
    /// registered user owns exactly one portfolio (established at registration), so this
    /// resolves the caller's portfolio for per-user data scoping (FR-03).
    /// </summary>
    Task<Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
