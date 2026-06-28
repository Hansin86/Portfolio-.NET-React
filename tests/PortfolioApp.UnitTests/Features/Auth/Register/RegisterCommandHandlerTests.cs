using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Features.Auth.Register;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.UnitTests.Features.Auth.Register;

public class RegisterCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_users, _passwordHasher, _tokenGenerator);
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_HashesPasswordAndPersistsUser()
    {
        _users.EmailExistsAsync("user@example.com", Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash("Sup3rSecretPass").Returns("hashed-password");

        var command = new RegisterCommand("user@example.com", "Sup3rSecretPass");

        await _handler.Handle(command, CancellationToken.None);

        _passwordHasher.Received(1).Hash("Sup3rSecretPass");
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Email == "user@example.com" &&
                user.PasswordHash == "hashed-password" &&
                user.IsDemoTemplate == false &&
                user.Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_CreatesPortfolioWithDefaultBaseCurrency()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        var command = new RegisterCommand("user@example.com", "Sup3rSecretPass");

        await _handler.Handle(command, CancellationToken.None);

        // The portfolio rides on the user aggregate so both persist in one unit of work.
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user =>
                user.Portfolios.Count == 1 &&
                user.Portfolios.Single().BaseCurrency == "USD" &&
                user.Portfolios.Single().Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailIsAvailable_ReturnsTokenAndUserIdentity()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _tokenGenerator.GenerateToken(Arg.Any<User>()).Returns("signed-jwt");

        var command = new RegisterCommand("user@example.com", "Sup3rSecretPass");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Token.Should().Be("signed-jwt");
        result.Email.Should().Be("user@example.com");
        result.UserId.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("  User@Example.COM  ", "user@example.com")]
    [InlineData("MixedCase@Mail.io", "mixedcase@mail.io")]
    public async Task Handle_NormalizesEmailBeforeUniquenessCheckAndPersistence(string input, string normalized)
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        var command = new RegisterCommand(input, "Sup3rSecretPass");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be(normalized);
        await _users.Received(1).EmailExistsAsync(normalized, Arg.Any<CancellationToken>());
        await _users.Received(1).AddAsync(
            Arg.Is<User>(user => user.Email == normalized),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ThrowsAndDoesNotPersist()
    {
        _users.EmailExistsAsync("taken@example.com", Arg.Any<CancellationToken>()).Returns(true);

        var command = new RegisterCommand("taken@example.com", "Sup3rSecretPass");

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<EmailAlreadyInUseException>();
        await _users.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        _passwordHasher.DidNotReceive().Hash(Arg.Any<string>());
    }
}
