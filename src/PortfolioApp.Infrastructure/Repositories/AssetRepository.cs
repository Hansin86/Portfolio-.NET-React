using Microsoft.EntityFrameworkCore;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Infrastructure.Persistence;

namespace PortfolioApp.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAssetRepository"/> over
/// <see cref="PortfolioDbContext"/>.
/// </summary>
public class AssetRepository : IAssetRepository
{
    private readonly PortfolioDbContext _context;

    public AssetRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public Task<Asset?> GetByTickerAsync(string ticker, CancellationToken cancellationToken = default)
    {
        string normalized = ticker.Trim().ToUpperInvariant();
        return _context.Assets.SingleOrDefaultAsync(a => a.Ticker == normalized, cancellationToken);
    }

    public async Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(asset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
