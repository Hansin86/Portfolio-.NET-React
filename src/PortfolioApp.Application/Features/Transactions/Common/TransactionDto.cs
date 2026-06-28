using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Application.Features.Transactions.Common;

/// <summary>
/// Read model for a portfolio transaction. Monetary values stay in their original
/// transaction currency (exposed as the ISO 4217 <see cref="Currency"/> code); conversion
/// to the portfolio's base currency happens at display time in later features.
/// </summary>
public record TransactionDto(
    Guid Id,
    string Ticker,
    string AssetName,
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    string Currency,
    DateOnly TransactionDate);
