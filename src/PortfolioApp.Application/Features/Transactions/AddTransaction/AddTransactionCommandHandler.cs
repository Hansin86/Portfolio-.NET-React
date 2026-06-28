using AutoMapper;
using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Application.Features.Transactions.AddTransaction;

/// <summary>
/// Handles <see cref="AddTransactionCommand"/>: resolves the caller's portfolio (FR-03),
/// gets-or-creates the asset by ticker, guards against over-selling, then persists the
/// transaction and returns it.
/// </summary>
public class AddTransactionCommandHandler : IRequestHandler<AddTransactionCommand, TransactionDto>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IPortfolioRepository _portfolios;
    private readonly IAssetRepository _assets;
    private readonly ITransactionRepository _transactions;
    private readonly IMapper _mapper;

    public AddTransactionCommandHandler(
        ICurrentUserService currentUser,
        IPortfolioRepository portfolios,
        IAssetRepository assets,
        ITransactionRepository transactions,
        IMapper mapper)
    {
        _currentUser = currentUser;
        _portfolios = portfolios;
        _assets = assets;
        _transactions = transactions;
        _mapper = mapper;
    }

    public async Task<TransactionDto> Handle(AddTransactionCommand request, CancellationToken cancellationToken)
    {
        Portfolio portfolio = await _portfolios.GetByUserIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("No portfolio was found for the current user.");

        Currency currency = Currency.From(request.Currency);
        Asset asset = await GetOrCreateAssetAsync(request, currency, cancellationToken);

        if (request.Type == TransactionType.Sell)
        {
            decimal held = await _transactions.GetHeldQuantityAsync(
                portfolio.Id, asset.Id, cancellationToken: cancellationToken);

            if (request.Quantity > held)
            {
                throw new DomainException(
                    $"Cannot sell {request.Quantity} units of {asset.Ticker}; only {held} are held.");
            }
        }

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = portfolio.Id,
            AssetId = asset.Id,
            Asset = asset,
            Type = request.Type,
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            Currency = currency,
            TransactionDate = request.TransactionDate,
            CreatedAt = DateTime.UtcNow,
        };

        await _transactions.AddAsync(transaction, cancellationToken);

        return _mapper.Map<TransactionDto>(transaction);
    }

    /// <summary>
    /// Returns the existing asset for the ticker, or creates one with interim metadata
    /// (name = ticker, quote currency = the transaction currency). Real metadata arrives
    /// with the market-data feature (FR-08).
    /// </summary>
    private async Task<Asset> GetOrCreateAssetAsync(
        AddTransactionCommand request,
        Currency currency,
        CancellationToken cancellationToken)
    {
        string ticker = request.Ticker.Trim().ToUpperInvariant();

        Asset? existing = await _assets.GetByTickerAsync(ticker, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Name = ticker,
            AssetType = request.AssetType ?? AssetType.Stock,
            QuoteCurrency = currency,
        };

        await _assets.AddAsync(asset, cancellationToken);
        return asset;
    }
}
