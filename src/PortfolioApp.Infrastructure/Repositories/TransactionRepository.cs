using Microsoft.EntityFrameworkCore;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Infrastructure.Persistence;

namespace PortfolioApp.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITransactionRepository"/> over
/// <see cref="PortfolioDbContext"/>.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly PortfolioDbContext _context;

    public TransactionRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Transactions
            .Include(t => t.Asset)
            .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<Transaction>> ListByPortfolioAsync(
        TransactionQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = _context.Transactions
            .Include(t => t.Asset)
            .Where(t => t.PortfolioId == parameters.PortfolioId);

        if (!string.IsNullOrWhiteSpace(parameters.AssetTicker))
        {
            string ticker = parameters.AssetTicker.Trim().ToUpperInvariant();
            query = query.Where(t => t.Asset.Ticker == ticker);
        }

        if (parameters.Type is TransactionType type)
        {
            query = query.Where(t => t.Type == type);
        }

        if (parameters.FromDate is DateOnly from)
        {
            query = query.Where(t => t.TransactionDate >= from);
        }

        if (parameters.ToDate is DateOnly to)
        {
            query = query.Where(t => t.TransactionDate <= to);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        query = ApplySort(query, parameters);

        List<Transaction> items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Transaction>(items, totalCount, parameters.Page, parameters.PageSize);
    }

    public async Task<decimal> GetHeldQuantityAsync(
        Guid portfolioId,
        Guid assetId,
        Guid? excludeTransactionId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = _context.Transactions
            .Where(t => t.PortfolioId == portfolioId && t.AssetId == assetId);

        if (excludeTransactionId is Guid excludeId)
        {
            query = query.Where(t => t.Id != excludeId);
        }

        decimal bought = await query
            .Where(t => t.Type == TransactionType.Buy)
            .SumAsync(t => (decimal?)t.Quantity, cancellationToken) ?? 0m;

        decimal sold = await query
            .Where(t => t.Type == TransactionType.Sell)
            .SumAsync(t => (decimal?)t.Quantity, cancellationToken) ?? 0m;

        return bought - sold;
    }

    /// <summary>
    /// Applies the requested sort plus a stable tie-breaker on <see cref="Transaction.Id"/>
    /// so paging stays deterministic when the primary key has duplicate values.
    /// </summary>
    private static IOrderedQueryable<Transaction> ApplySort(
        IQueryable<Transaction> query,
        TransactionQueryParameters parameters)
    {
        bool desc = parameters.Descending;

        IOrderedQueryable<Transaction> ordered = parameters.SortBy switch
        {
            TransactionSortField.Ticker => desc
                ? query.OrderByDescending(t => t.Asset.Ticker)
                : query.OrderBy(t => t.Asset.Ticker),
            TransactionSortField.Quantity => desc
                ? query.OrderByDescending(t => t.Quantity)
                : query.OrderBy(t => t.Quantity),
            TransactionSortField.PricePerUnit => desc
                ? query.OrderByDescending(t => t.PricePerUnit)
                : query.OrderBy(t => t.PricePerUnit),
            TransactionSortField.Type => desc
                ? query.OrderByDescending(t => t.Type)
                : query.OrderBy(t => t.Type),
            _ => desc
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate),
        };

        return ordered.ThenBy(t => t.Id);
    }
}
