using FluentValidation;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Application.Features.Transactions.AddTransaction;

/// <summary>
/// Input rules for <see cref="AddTransactionCommand"/>, enforced by the validation pipeline
/// behaviour before the handler runs.
/// </summary>
public class AddTransactionCommandValidator : AbstractValidator<AddTransactionCommand>
{
    public AddTransactionCommandValidator()
    {
        RuleFor(command => command.Ticker)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(command => command.Type)
            .IsInEnum();

        RuleFor(command => command.Quantity)
            .GreaterThan(0);

        RuleFor(command => command.PricePerUnit)
            .GreaterThan(0);

        RuleFor(command => command.Currency)
            .NotEmpty()
            .Must(BeAKnownCurrency)
                .WithMessage("'{PropertyValue}' is not a recognised ISO 4217 currency code.");

        RuleFor(command => command.TransactionDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Transaction date must not be in the future.");

        RuleFor(command => command.AssetType!.Value)
            .IsInEnum()
            .When(command => command.AssetType.HasValue);
    }

    private static bool BeAKnownCurrency(string? code)
        => !string.IsNullOrWhiteSpace(code) && Iso4217.Codes.Contains(code.Trim().ToUpperInvariant());
}
