namespace PortfolioApp.Application.Features.Transactions.Common;

/// <summary>
/// Fields a transaction list can be sorted by (FR-07). Maps to columns on the
/// transaction / its asset in the persistence query.
/// </summary>
public enum TransactionSortField
{
    TransactionDate,
    Ticker,
    Quantity,
    PricePerUnit,
    Type
}
