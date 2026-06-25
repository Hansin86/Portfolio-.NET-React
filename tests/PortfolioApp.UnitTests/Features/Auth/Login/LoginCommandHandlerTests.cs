using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Features.Auth.Login;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.UnitTests.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_users, _passwordHasher, _tokenGenerator);
    }

    private static User ExistingUser(string email = "user@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = "stored-hash",
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenAndIdentity()
    {
        User user = ExistingUser();
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("correct-password", "stored-hash").Returns(true);
        _tokenGenerator.GenerateToken(user).Returns("signed-jwt");

        var command = new LoginCommand("user@example.com", "correct-password");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Token.Should().Be("signed-jwt");
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task Handle_NormalizesEmailBeforeLookup()
    {
        User user = ExistingUser();
        _users.GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var command = new LoginCommand("  User@Example.COM  ", "correct-password");

        await _handler.Handle(command, CancellationToken.None);

        await _users.Received(1).GetByEmailAsync("user@example.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsInvalidCredentials()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var command = new LoginCommand("missing@example.com", "any-password");

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WhenPasswordDoesNotMatch_ThrowsInvalidCredentials()
    {
        User user = ExistingUser();
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong-password", "stored-hash").Returns(false);

        var command = new LoginCommand("user@example.com", "wrong-password");

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _tokenGenerator.DidNotReceive().GenerateToken(Arg.Any<User>());
    }
}
