using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Persistence port for <see cref="User"/> aggregates. Implemented in Infrastructure over
/// EF Core / PostgreSQL.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Returns the user with the given email, or <c>null</c> if none exists. Used by login
    /// to locate the account.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if a user already exists with the given email. Used by registration to
    /// enforce email uniqueness.
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user and persists the change.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
