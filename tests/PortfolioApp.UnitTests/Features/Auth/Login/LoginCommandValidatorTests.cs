using FluentAssertions;
using PortfolioApp.Application.Features.Auth.Login;

namespace PortfolioApp.UnitTests.Features.Auth.Login;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmailAndPasswordPresent_Passes()
    {
        var command = new LoginCommand("user@example.com", "any-password");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "any-password")]
    [InlineData("not-an-email", "any-password")]
    [InlineData("user@example.com", "")]
    public void Validate_WithMissingOrInvalidFields_Fails(string email, string password)
    {
        var command = new LoginCommand(email, password);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
