using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A known tradable instrument (stock or ETF), shared across all portfolios.
/// </summary>
public class Asset
{
    public Guid Id { get; set; }

    /// <summary>Unique ticker symbol, e.g. AAPL, VWCE, MSFT.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Human-readable name, e.g. Apple Inc.</summary>
    public string Name { get; set; } = string.Empty;

    public AssetType AssetType { get; set; }

    /// <summary>ISO 4217 currency the asset is quoted in.</summary>
    public Currency QuoteCurrency { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<PriceSnapshot> PriceSnapshots { get; set; } = new List<PriceSnapshot>();
}
