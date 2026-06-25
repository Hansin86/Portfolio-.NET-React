namespace PortfolioApp.Domain.Exceptions;

/// <summary>
/// Thrown when login fails because the email is unknown or the password does not match
/// (FR-02). The message is deliberately generic so it cannot be used to probe which emails
/// are registered. Surfaced as a 401 Unauthorized by the API's global exception middleware.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The email or password provided is incorrect.")
    {
    }
}
