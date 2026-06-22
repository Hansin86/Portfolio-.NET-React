namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A point-in-time foreign exchange rate, fetched from Alpha Vantage. Append-only:
/// existing rows are never updated. Used to convert asset values to a base currency.
/// </summary>
public class FxRate
{
    public Guid Id { get; set; }

    /// <summary>ISO 4217 source currency, e.g. USD.</summary>
    public string FromCurrency { get; set; } = string.Empty;

    /// <summary>ISO 4217 target currency, e.g. PLN.</summary>
    public string ToCurrency { get; set; } = string.Empty;

    /// <summary>Units of <see cref="ToCurrency"/> per one unit of <see cref="FromCurrency"/>.</summary>
    public decimal Rate { get; set; }

    /// <summary>Timestamp of the fetched rate.</summary>
    public DateTime RecordedAt { get; set; }
}
