using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Common.Mappings;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Features.Transactions.GetTransactionById;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Features.Transactions.GetTransactionById;

public class GetTransactionByIdQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPortfolioRepository _portfolios = Substitute.For<IPortfolioRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionByIdQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Portfolio _portfolio;

    public GetTransactionByIdQueryHandlerTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<TransactionProfile>(),
            NullLoggerFactory.Instance);
        IMapper mapper = config.CreateMapper();

        _handler = new GetTransactionByIdQueryHandler(_currentUser, _portfolios, _transactions, mapper);

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
        TransactionDate = new DateOnly(2026, 5, 1),
    };

    [Fact]
    public async Task Handle_WhenOwnedByCaller_ReturnsDto()
    {
        Transaction transaction = TransactionIn(_portfolio.Id);
        _transactions.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);

        TransactionDto result = await _handler.Handle(
            new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        result.Id.Should().Be(transaction.Id);
        result.Ticker.Should().Be("AAPL");
    }

    [Fact]
    public async Task Handle_WhenTransactionMissing_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _transactions.GetByIdAsync(id, Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = () => _handler.Handle(new GetTransactionByIdQuery(id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenOwnedByAnotherPortfolio_ThrowsNotFound()
    {
        Transaction otherUsersTransaction = TransactionIn(Guid.NewGuid());
        _transactions.GetByIdAsync(otherUsersTransaction.Id, Arg.Any<CancellationToken>())
            .Returns(otherUsersTransaction);

        Func<Task> act = () => _handler.Handle(
            new GetTransactionByIdQuery(otherUsersTransaction.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
