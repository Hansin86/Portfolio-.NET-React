using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Infrastructure.Persistence.Converters;

/// <summary>
/// EF Core value converter mapping a <see cref="Currency"/> to its three-letter code for
/// storage and back. Applied to every <see cref="Currency"/> property via a convention in
/// <see cref="PortfolioDbContext.ConfigureConventions"/>.
/// </summary>
public class CurrencyConverter : ValueConverter<Currency, string>
{
    public CurrencyConverter()
        : base(
            currency => currency.Code,
            code => Currency.From(code))
    {
    }
}
