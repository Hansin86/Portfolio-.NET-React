namespace PortfolioApp.Domain.Enums;

/// <summary>
/// Category of a tradable <see cref="Entities.Asset"/>.
/// Persisted as a string ("STOCK" / "ETF") via EF Core value conversion.
/// </summary>
public enum AssetType
{
    Stock,
    Etf
}
