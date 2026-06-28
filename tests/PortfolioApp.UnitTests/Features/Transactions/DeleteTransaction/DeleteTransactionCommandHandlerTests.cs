using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Features.Transactions.DeleteTransaction;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Features.Transactions.DeleteTransaction;

public class DeleteTransactionCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPortfolioRepository _portfolios = Substitute.For<IPortfolioRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly DeleteTransactionCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Portfolio _portfolio;

    public DeleteTransactionCommandHandlerTests()
    {
        _handler = new DeleteTransactionCommandHandler(_currentUser, _portfolios, _transactions);

        _portfolio = new Portfolio { Id = Guid.NewGuid(), UserId = _userId, BaseCurrency = Currency.Usd };

        _currentUser.UserId.Returns(_userId);
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(_portfolio);
    }

    private Transaction TransactionIn(Guid portfolioId) => new()
    {
        Id = Guid.NewGuid(),
        PortfolioId = portfolioId,
        Asset = new Asset { Id = Guid.NewGuid(), Ticker = "AAPL", Name = "Apple Inc." },
        Type = TransactionType.Buy,
        Quantity = 10m,
        PricePerUnit = 150m,
        Currency = Currency.Usd,
        TransactionDate = new DateOnly(2026, 1, 1),
    };

    [Fact]
    public async Task Handle_WhenOwnedByCaller_Removes()
    {
        Transaction transaction = TransactionIn(_portfolio.Id);
        _transactions.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        await _handler.Handle(new DeleteTransactionCommand(transaction.Id), CancellationToken.None);

        await _transactions.Received(1).RemoveAsync(transaction, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTransactionMissing_ThrowsNotFoundAndDoesNotRemove()
    {
        var id = Guid.NewGuid();
        _transactions.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = () => _handler.Handle(new DeleteTransactionCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().RemoveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherPortfolio_ThrowsNotFound()
    {
        Transaction otherUsers = TransactionIn(Guid.NewGuid());
        _transactions.GetByIdAsync(otherUsers.Id, Arg.Any<CancellationToken>()).Returns(otherUsers);

        Func<Task> act = () => _handler.Handle(new DeleteTransactionCommand(otherUsers.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().RemoveAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }
}
