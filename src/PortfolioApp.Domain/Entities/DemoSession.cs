namespace PortfolioApp.Domain.Entities;

/// <summary>
/// One active demo login. Owns an isolated <see cref="Portfolio"/> copy that is
/// cascade-deleted when the session expires (cleaned up by a Hangfire job).
/// </summary>
public class DemoSession
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }
    public Portfolio Portfolio { get; set; } = null!;

    /// <summary>When the session expires (created_at + 60 minutes).</summary>
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
