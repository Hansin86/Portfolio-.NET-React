using FluentValidation;

namespace PortfolioApp.Application.Features.Auth.Login;

/// <summary>
/// Input rules for <see cref="LoginCommand"/>. Only presence is checked here — credential
/// correctness is verified by the handler so failures stay indistinguishable.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(command => command.Password)
            .NotEmpty();
    }
}
