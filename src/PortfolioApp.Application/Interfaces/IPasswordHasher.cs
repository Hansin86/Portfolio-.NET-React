namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Hashes and verifies user passwords. Implemented in Infrastructure with bcrypt
/// (NFR-03). Plain-text passwords never leave the Application boundary.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted hash for the given plain-text password.</summary>
    string Hash(string password);

    /// <summary>
    /// Returns true if <paramref name="password"/> matches the stored
    /// <paramref name="passwordHash"/>.
    /// </summary>
    bool Verify(string password, string passwordHash);
}
