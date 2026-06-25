namespace PortfolioApp.Domain.Exceptions;

/// <summary>
/// Thrown when registration is attempted with an email that already belongs to an
/// existing account (FR-01). Surfaced as a 409 Conflict by the API's global exception
/// middleware.
/// </summary>
public class EmailAlreadyInUseException : Exception
{
    public EmailAlreadyInUseException(string email)
        : base($"An account with email '{email}' already exists.")
    {
    }
}
