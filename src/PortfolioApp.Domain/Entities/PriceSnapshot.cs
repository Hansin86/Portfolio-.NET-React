using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A point-in-time market price for an asset, fetched from Alpha Vantage.
/// Append-only: existing rows are never updated. Used for charts and P&amp;L history.
/// </summary>
public class PriceSnapshot
{
    public Guid Id { get; set; }

    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public decimal Price { get; set; }

    /// <summary>ISO 4217 currency, matching the asset's quote currency.</summary>
    public Currency Currency { get; set; } = null!;

    /// <summary>Timestamp of the fetched price.</summary>
    public DateTime RecordedAt { get; set; }
}
