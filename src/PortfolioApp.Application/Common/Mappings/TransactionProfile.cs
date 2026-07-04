using AutoMapper;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Domain.Entities;

namespace PortfolioApp.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for <see cref="Transaction"/> read models. Flattens the related
/// <see cref="Asset"/> (ticker, name) and exposes the currency as its ISO 4217 code string.
/// </summary>
public class TransactionProfile : Profile
{
    public TransactionProfile()
    {
        CreateMap<Transaction, TransactionDto>()
            .ForCtorParam(nameof(TransactionDto.Ticker), o => o.MapFrom(t => t.Asset.Ticker))
            .ForCtorParam(nameof(TransactionDto.AssetName), o => o.MapFrom(t => t.Asset.Name))
            .ForCtorParam(nameof(TransactionDto.Currency), o => o.MapFrom(t => t.Currency.Code));
    }
}
