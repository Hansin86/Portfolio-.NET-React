using FluentValidation;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.Application.Features.Transactions.EditTransaction;

/// <summary>
/// Input rules for <see cref="EditTransactionCommand"/>, enforced by the validation pipeline
/// behaviour before the handler runs. Mirrors the add rules for the editable fields.
/// </summary>
public class EditTransactionCommandValidator : AbstractValidator<EditTransactionCommand>
{
    public EditTransactionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

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
    }

    private static bool BeAKnownCurrency(string? code)
        => !string.IsNullOrWhiteSpace(code) && Iso4217.Codes.Contains(code.Trim().ToUpperInvariant());
}
