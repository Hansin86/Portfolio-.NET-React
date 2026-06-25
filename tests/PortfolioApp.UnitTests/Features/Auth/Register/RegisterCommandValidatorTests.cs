using FluentAssertions;
using PortfolioApp.Application.Features.Auth.Register;

namespace PortfolioApp.UnitTests.Features.Auth.Register;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    private const string ValidPassword = "Sup3rSecretPass";

    [Fact]
    public void Validate_WithValidEmailAndPassword_Passes()
    {
        var command = new RegisterCommand("user@example.com", ValidPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var command = new RegisterCommand(email, ValidPassword);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RegisterCommand.Email));
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("Sh0rt", "too short")]
    [InlineData("alllowercase12", "no uppercase")]
    [InlineData("ALLUPPERCASE12", "no lowercase")]
    [InlineData("NoDigitsHereee", "no digit")]
    [InlineData("Has Space12345", "contains whitespace")]
    public void Validate_WithPasswordViolatingPolicy_Fails(string password, string reason)
    {
        var command = new RegisterCommand("user@example.com", password);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse(because: reason);
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RegisterCommand.Password));
    }

    [Fact]
    public void Validate_WhenPasswordExceedsMaxLength_Fails()
    {
        string password = "Aa1" + new string('x', 130);
        var command = new RegisterCommand("user@example.com", password);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(RegisterCommand.Password));
    }
}
