using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Interfaces;

/// <summary>
/// Issues signed JWT bearer tokens for authenticated users. Implemented in
/// Infrastructure; signing key, issuer, and audience come from configuration.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT for the given <paramref name="user"/>, carrying the claims
    /// downstream endpoints use for authorization and per-user data scoping (FR-03).
    /// </summary>
    string GenerateToken(User user);
}
