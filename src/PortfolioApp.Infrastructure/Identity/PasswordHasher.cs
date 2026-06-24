using PortfolioApp.Application.Interfaces;

namespace PortfolioApp.Infrastructure.Identity;

/// <summary>
/// bcrypt-based <see cref="IPasswordHasher"/> implementation (NFR-03). Salting is handled
/// internally by bcrypt and embedded in the resulting hash.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash)
        => BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
