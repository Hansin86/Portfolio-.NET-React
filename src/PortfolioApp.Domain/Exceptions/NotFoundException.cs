namespace PortfolioApp.Domain.Exceptions;

/// <summary>
/// Thrown when a requested resource does not exist <em>or is not owned by the caller</em>.
/// Surfaced as a 404 Not Found by the API's global exception middleware — deliberately the
/// same response in both cases so a caller cannot probe for the existence of another user's
/// data (FR-03).
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with id '{key}' was not found.")
    {
    }
}
