namespace PortfolioApp.Domain.Enums;

/// <summary>
/// Direction of a portfolio <see cref="Entities.Transaction"/>.
/// Persisted as a string ("BUY" / "SELL") via EF Core value conversion.
/// </summary>
public enum TransactionType
{
    Buy,
    Sell
}
