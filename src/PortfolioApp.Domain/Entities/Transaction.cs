using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A single buy or sell operation on a portfolio. Monetary values are stored in their
/// original transaction currency and never converted at rest.
/// </summary>
public class Transaction
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;

    public Guid AssetId { get; set; }
    public Asset Asset { get; set; } = null!;

    public TransactionType Type { get; set; }

    /// <summary>Number of units traded. Always positive.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Price per unit, expressed in <see cref="Currency"/>.</summary>
    public decimal PricePerUnit { get; set; }

    /// <summary>ISO 4217 currency of <see cref="PricePerUnit"/>.</summary>
    public Currency Currency { get; set; } = null!;

    /// <summary>Date the trade took place (distinct from <see cref="CreatedAt"/>).</summary>
    public DateOnly TransactionDate { get; set; }

    public DateTime CreatedAt { get; set; }
}
