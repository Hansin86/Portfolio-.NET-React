using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Common.Mappings;
using PortfolioApp.Application.Features.Transactions.AddTransaction;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Features.Transactions.AddTransaction;

public class AddTransactionCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPortfolioRepository _portfolios = Substitute.For<IPortfolioRepository>();
    private readonly IAssetRepository _assets = Substitute.For<IAssetRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly IMapper _mapper;
    private readonly AddTransactionCommandHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Portfolio _portfolio;

    public AddTransactionCommandHandlerTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<TransactionProfile>(),
            NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _handler = new AddTransactionCommandHandler(
            _currentUser, _portfolios, _assets, _transactions, _mapper);

        _portfolio = new Portfolio { Id = Guid.NewGuid(), UserId = _userId, BaseCurrency = Currency.Usd };

        _currentUser.UserId.Returns(_userId);
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(_portfolio);
    }

    private static AddTransactionCommand BuyCommand(string ticker = "AAPL", decimal quantity = 10m) =>
        new(ticker, TransactionType.Buy, quantity, 150m, "USD",
            DateOnly.FromDateTime(DateTime.UtcNow));

    [Fact]
    public async Task Handle_WhenAssetDoesNotExist_CreatesAssetWithInterimMetadata()
    {
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).ReturnsNull();

        await _handler.Handle(BuyCommand(), CancellationToken.None);

        await _assets.Received(1).AddAsync(
            Arg.Is<Asset>(a =>
                a.Ticker == "AAPL" &&
                a.Name == "AAPL" &&
                a.AssetType == AssetType.Stock &&
                a.QuoteCurrency == Currency.From("USD") &&
                a.Id != Guid.Empty),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesTickerToUpperCase()
    {
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).ReturnsNull();

        await _handler.Handle(BuyCommand(ticker: "  aapl  "), CancellationToken.None);

        await _assets.Received(1).GetByTickerAsync("AAPL", Arg.Any<CancellationToken>());
        await _assets.Received(1).AddAsync(
            Arg.Is<Asset>(a => a.Ticker == "AAPL"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAssetExists_ReusesItAndDoesNotCreate()
    {
        var existing = new Asset
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Name = "Apple Inc.",
            AssetType = AssetType.Stock,
            QuoteCurrency = Currency.Usd,
        };
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).Returns(existing);

        await _handler.Handle(BuyCommand(), CancellationToken.None);

        await _assets.DidNotReceive().AddAsync(Arg.Any<Asset>(), Arg.Any<CancellationToken>());
        await _transactions.Received(1).AddAsync(
            Arg.Is<Transaction>(t => t.AssetId == existing.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PersistsTransactionScopedToCallersPortfolio()
    {
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await _handler.Handle(BuyCommand(quantity: 10m), CancellationToken.None);

        await _transactions.Received(1).AddAsync(
            Arg.Is<Transaction>(t =>
                t.PortfolioId == _portfolio.Id &&
                t.Type == TransactionType.Buy &&
                t.Quantity == 10m &&
                t.PricePerUnit == 150m &&
                t.Currency == Currency.From("USD") &&
                t.Id != Guid.Empty),
            Arg.Any<CancellationToken>());

        result.Ticker.Should().Be("AAPL");
        result.Currency.Should().Be("USD");
        result.Quantity.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_WhenNoPortfolioForUser_ThrowsNotFound()
    {
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = () => _handler.Handle(BuyCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSellExceedsHeldQuantity_ThrowsDomainExceptionAndDoesNotPersist()
    {
        var existing = new Asset
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Name = "Apple Inc.",
            AssetType = AssetType.Stock,
            QuoteCurrency = Currency.Usd,
        };
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).Returns(existing);
        _transactions.GetHeldQuantityAsync(
                _portfolio.Id, existing.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(3m);

        var sell = new AddTransactionCommand("AAPL", TransactionType.Sell, 5m, 150m, "USD",
            DateOnly.FromDateTime(DateTime.UtcNow));

        Func<Task> act = () => _handler.Handle(sell, CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _transactions.DidNotReceive().AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSellWithinHeldQuantity_Persists()
    {
        var existing = new Asset
        {
            Id = Guid.NewGuid(),
            Ticker = "AAPL",
            Name = "Apple Inc.",
            AssetType = AssetType.Stock,
            QuoteCurrency = Currency.Usd,
        };
        _assets.GetByTickerAsync("AAPL", Arg.Any<CancellationToken>()).Returns(existing);
        _transactions.GetHeldQuantityAsync(
                _portfolio.Id, existing.Id, Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(10m);

        var sell = new AddTransactionCommand("AAPL", TransactionType.Sell, 4m, 150m, "USD",
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _handler.Handle(sell, CancellationToken.None);

        await _transactions.Received(1).AddAsync(
            Arg.Is<Transaction>(t => t.Type == TransactionType.Sell && t.Quantity == 4m),
            Arg.Any<CancellationToken>());
    }
}
