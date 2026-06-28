using FluentAssertions;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Domain.ValueObjects;

public class CurrencyTests
{
    [Theory]
    [InlineData("USD")]
    [InlineData("eur")]
    [InlineData("  gbp  ")]
    public void From_WithRecognisedCode_NormalisesToUpperCase(string input)
    {
        Currency currency = Currency.From(input);

        currency.Code.Should().Be(input.Trim().ToUpperInvariant());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("ZZZ")]
    [InlineData("123")]
    public void From_WithUnrecognisedOrEmptyCode_Throws(string input)
    {
        Action act = () => Currency.From(input);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_IsByCode()
    {
        Currency.From("usd").Should().Be(Currency.From("USD"));
        Currency.From("USD").Should().Be(Currency.Usd);
        Currency.From("USD").Should().NotBe(Currency.From("EUR"));
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        Currency.From("PLN").ToString().Should().Be("PLN");
    }
}
