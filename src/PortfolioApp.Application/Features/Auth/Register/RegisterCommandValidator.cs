using FluentValidation;

namespace PortfolioApp.Application.Features.Auth.Register;

/// <summary>
/// Input rules for <see cref="RegisterCommand"/>, enforced by the validation pipeline
/// behaviour before the handler runs.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .MaximumLength(128).WithMessage("Password must not exceed 128 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Must(password => password is null || !password.Any(char.IsWhiteSpace))
                .WithMessage("Password must not contain whitespace.");
    }
}
