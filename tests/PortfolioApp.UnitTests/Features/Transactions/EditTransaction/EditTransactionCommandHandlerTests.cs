using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Common.Mappings;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Features.Transactions.EditTransaction;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Features.Transactions.EditTransaction;

public class EditTransactionCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPortfolioRepository _portfolios = Substitute.For<IPortfolioRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly EditTransactionCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Portfolio _portfolio;
    private readonly Asset _asset;

    public EditTransactionCommandHandlerTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<TransactionProfile>(),
            NullLoggerFactory.Instance);
        IMapper mapper = config.CreateMapper();

        _handler = new EditTransactionCommandHandler(_currentUser, _portfolios, _transactions, mapper);

        _portfolio = new Portfolio { Id = Guid.NewGuid(), UserId = _userId, BaseCurrency = Currency.Usd };
        _asset = new Asset { Id = Guid.NewGuid(), Ticker = "AAPL", Name = "Apple Inc." };

        _currentUser.UserId.Returns(_userId);
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(_portfolio);
    }

    private Transaction ExistingTransaction(Guid portfolioId) => new()
    {
        Id = Guid.NewGuid(),
        PortfolioId = portfolioId,
        AssetId = _asset.Id,
        Asset = _asset,
        Type = TransactionType.Buy,
        Quantity = 10m,
        PricePerUnit = 150m,
        Currency = Currency.Usd,
        TransactionDate = new DateOnly(2026, 1, 1),
    };

    private static EditTransactionCommand Command(Guid id, TransactionType type = TransactionType.Buy, decimal quantity = 8m) =>
        new(id, type, quantity, 160m, "EUR", new DateOnly(2026, 2, 1));

    [Fact]
    public async Task Handle_WhenOwnedByCaller_AppliesEditAndPersists()
    {
        Transaction transaction = ExistingTransaction(_portfolio.Id);
        _transactions.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _transactions.GetHeldQuantityAsync(
                _portfolio.Id, _asset.Id, transaction.Id, Arg.Any<CancellationToken>())
            .Returns(0m);

        TransactionDto result = await _handler.Handle(Command(transaction.Id), CancellationToken.None);

        transaction.Quantity.Should().Be(8m);
        transaction.PricePerUnit.Should().Be(160m);
        transaction.Currency.Should().Be(Currency.From("EUR"));
        transaction.TransactionDate.Should().Be(new DateOnly(2026, 2, 1));
        await _transactions.Received(1).UpdateAsync(transaction, Arg.Any<CancellationToken>());
        result.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Handle_WhenTransactionMissing_ThrowsNotFoundAndDoesNotPersist()
    {
        var id = Guid.NewGuid();
        _transactions.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = () => _handler.Handle(Command(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().UpdateAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherPortfolio_ThrowsNotFound()
    {
        Transaction otherUsers = ExistingTransaction(Guid.NewGuid());
        _transactions.GetByIdAsync(otherUsers.Id, Arg.Any<CancellationToken>()).Returns(otherUsers);

        Func<Task> act = () => _handler.Handle(Command(otherUsers.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().UpdateAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEditWouldDriveHoldingNegative_ThrowsDomainException()
    {
        Transaction transaction = ExistingTransaction(_portfolio.Id);
        _transactions.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        // Other rows leave 3 units held (excluding this one); selling 5 would net -2.
        _transactions.GetHeldQuantityAsync(
                _portfolio.Id, _asset.Id, transaction.Id, Arg.Any<CancellationToken>())
            .Returns(3m);

        EditTransactionCommand sell = Command(transaction.Id, TransactionType.Sell, quantity: 5m);

        Func<Task> act = () => _handler.Handle(sell, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _transactions.DidNotReceive().UpdateAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSellWithinOtherHoldings_Persists()
    {
        Transaction transaction = ExistingTransaction(_portfolio.Id);
        _transactions.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _transactions.GetHeldQuantityAsync(
                _portfolio.Id, _asset.Id, transaction.Id, Arg.Any<CancellationToken>())
            .Returns(10m);

        await _handler.Handle(Command(transaction.Id, TransactionType.Sell, quantity: 4m), CancellationToken.None);

        await _transactions.Received(1).UpdateAsync(transaction, Arg.Any<CancellationToken>());
    }
}
