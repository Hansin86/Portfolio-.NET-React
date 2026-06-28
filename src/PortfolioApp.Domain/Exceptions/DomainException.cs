namespace PortfolioApp.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant or business rule is violated (e.g. an unrecognised
/// currency code, or selling more units than are held). Surfaced as a
/// 422 Unprocessable Entity by the API's global exception middleware.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
