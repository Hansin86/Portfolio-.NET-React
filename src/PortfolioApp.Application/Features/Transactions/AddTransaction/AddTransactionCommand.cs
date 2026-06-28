using MediatR;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.Application.Features.Transactions.AddTransaction;

/// <summary>
/// Records a buy or sell against the caller's portfolio (FR-05). The asset is resolved (or
/// created) from <see cref="Ticker"/>; <see cref="AssetType"/> seeds a newly created asset
/// and is ignored when the asset already exists.
/// </summary>
public record AddTransactionCommand(
    string Ticker,
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    string Currency,
    DateOnly TransactionDate,
    AssetType? AssetType = null) : IRequest<TransactionDto>;
