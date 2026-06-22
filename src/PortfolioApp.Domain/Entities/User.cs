namespace PortfolioApp.Domain.Entities;

/// <summary>
/// A registered application user. A single seed row with
/// <see cref="IsDemoTemplate"/> set to true holds the read-only demo portfolio template.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Login email. Null for the demo template row.</summary>
    public string? Email { get; set; }

    /// <summary>bcrypt password hash.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>True for the single demo template seed row.</summary>
    public bool IsDemoTemplate { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<Portfolio> Portfolios { get; set; } = new List<Portfolio>();
}
