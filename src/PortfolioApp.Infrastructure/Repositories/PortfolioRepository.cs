using Microsoft.EntityFrameworkCore;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Infrastructure.Persistence;

namespace PortfolioApp.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IPortfolioRepository"/> over
/// <see cref="PortfolioDbContext"/>.
/// </summary>
public class PortfolioRepository : IPortfolioRepository
{
    private readonly PortfolioDbContext _context;

    public PortfolioRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public Task<Portfolio?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.Portfolios.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);
}
