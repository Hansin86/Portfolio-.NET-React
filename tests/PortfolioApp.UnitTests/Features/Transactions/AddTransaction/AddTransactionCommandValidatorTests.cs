using FluentValidation.TestHelper;
using PortfolioApp.Application.Features.Transactions.AddTransaction;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.UnitTests.Features.Transactions.AddTransaction;

public class AddTransactionCommandValidatorTests
{
    private readonly AddTransactionCommandValidator _validator = new();

    private static AddTransactionCommand Valid() =>
        new("AAPL", TransactionType.Buy, 10m, 150m, "USD",
            DateOnly.FromDateTime(DateTime.UtcNow), AssetType.Stock);

    [Fact]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("THIS_TICKER_IS_WAY_TOO_LONG")]
    public void Invalid_ticker_fails(string ticker)
    {
        _validator.TestValidate(Valid() with { Ticker = ticker })
            .ShouldHaveValidationErrorFor(c => c.Ticker);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        _validator.TestValidate(Valid() with { Quantity = quantity })
            .ShouldHaveValidationErrorFor(c => c.Quantity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Non_positive_price_fails(decimal price)
    {
        _validator.TestValidate(Valid() with { PricePerUnit = price })
            .ShouldHaveValidationErrorFor(c => c.PricePerUnit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    [InlineData("ZZZ")]
    public void Invalid_currency_fails(string currency)
    {
        _validator.TestValidate(Valid() with { Currency = currency })
            .ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData(" gbp ")]
    public void Known_currency_passes(string currency)
    {
        _validator.TestValidate(Valid() with { Currency = currency })
            .ShouldNotHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Future_transaction_date_fails()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        _validator.TestValidate(Valid() with { TransactionDate = future })
            .ShouldHaveValidationErrorFor(c => c.TransactionDate);
    }

    [Fact]
    public void Undefined_type_fails()
    {
        _validator.TestValidate(Valid() with { Type = (TransactionType)99 })
            .ShouldHaveValidationErrorFor(c => c.Type);
    }
}
