using FluentValidation.TestHelper;
using PortfolioApp.Application.Features.Transactions.EditTransaction;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.UnitTests.Features.Transactions.EditTransaction;

public class EditTransactionCommandValidatorTests
{
    private readonly EditTransactionCommandValidator _validator = new();

    private static EditTransactionCommand Valid() =>
        new(Guid.NewGuid(), TransactionType.Buy, 10m, 150m, "USD",
            DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_id_fails()
    {
        _validator.TestValidate(Valid() with { Id = Guid.Empty })
            .ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        _validator.TestValidate(Valid() with { Quantity = quantity })
            .ShouldHaveValidationErrorFor(c => c.Quantity);
    }

    [Fact]
    public void Invalid_currency_fails()
    {
        _validator.TestValidate(Valid() with { Currency = "ZZZ" })
            .ShouldHaveValidationErrorFor(c => c.Currency);
    }

    [Fact]
    public void Future_transaction_date_fails()
    {
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        _validator.TestValidate(Valid() with { TransactionDate = future })
            .ShouldHaveValidationErrorFor(c => c.TransactionDate);
    }
}
