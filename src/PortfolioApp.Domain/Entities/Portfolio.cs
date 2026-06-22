namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A user's collection of transactions. Each registered user owns one portfolio;
/// every demo session gets a temporary isolated copy with <see cref="UserId"/> null.
/// </summary>
public class Portfolio
{
    public Guid Id { get; set; }

    /// <summary>Owning user. Null for demo-session portfolios.</summary>
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>ISO 4217 base display currency, e.g. PLN, USD, EUR.</summary>
    public string BaseCurrency { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>Demo session linked to this portfolio, if it is a demo copy.</summary>
    public DemoSession? DemoSession { get; set; }
}
