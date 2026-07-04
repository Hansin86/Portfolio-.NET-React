using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using PortfolioApp.Application.Common.Mappings;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Features.Transactions.GetTransactions;
using PortfolioApp.Application.Interfaces;
using PortfolioApp.Domain.Entities;
using PortfolioApp.Domain.Enums;
using PortfolioApp.Domain.Exceptions;
using PortfolioApp.Domain.ValueObjects;

namespace PortfolioApp.UnitTests.Features.Transactions.GetTransactions;

public class GetTransactionsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPortfolioRepository _portfolios = Substitute.For<IPortfolioRepository>();
    private readonly ITransactionRepository _transactions = Substitute.For<ITransactionRepository>();
    private readonly GetTransactionsQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Portfolio _portfolio;

    public GetTransactionsQueryHandlerTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<TransactionProfile>(),
            NullLoggerFactory.Instance);
        IMapper mapper = config.CreateMapper();

        _handler = new GetTransactionsQueryHandler(_currentUser, _portfolios, _transactions, mapper);

        _portfolio = new Portfolio { Id = Guid.NewGuid(), UserId = _userId, BaseCurrency = Currency.Usd };

        _currentUser.UserId.Returns(_userId);
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(_portfolio);
        _transactions.ListByPortfolioAsync(Arg.Any<TransactionQueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Transaction>(Array.Empty<Transaction>(), 0, 1, 20));
    }

    [Fact]
    public async Task Handle_ScopesQueryToCallersPortfolioAndForwardsFilters()
    {
        var query = new GetTransactionsQuery(
            AssetTicker: "AAPL",
            Type: TransactionType.Buy,
            FromDate: new DateOnly(2026, 1, 1),
            ToDate: new DateOnly(2026, 6, 1),
            SortBy: TransactionSortField.Quantity,
            Descending: false,
            Page: 2,
            PageSize: 50);

        await _handler.Handle(query, CancellationToken.None);

        await _transactions.Received(1).ListByPortfolioAsync(
            Arg.Is<TransactionQueryParameters>(p =>
                p.PortfolioId == _portfolio.Id &&
                p.AssetTicker == "AAPL" &&
                p.Type == TransactionType.Buy &&
                p.FromDate == new DateOnly(2026, 1, 1) &&
                p.ToDate == new DateOnly(2026, 6, 1) &&
                p.SortBy == TransactionSortField.Quantity &&
                p.Descending == false &&
                p.Page == 2 &&
                p.PageSize == 50),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsPagedEntitiesToDtosPreservingPagingMetadata()
    {
        var asset = new Asset { Id = Guid.NewGuid(), Ticker = "AAPL", Name = "Apple Inc." };
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            PortfolioId = _portfolio.Id,
            Asset = asset,
            Type = TransactionType.Buy,
            Quantity = 10m,
            PricePerUnit = 150m,
            Currency = Currency.Usd,
            TransactionDate = new DateOnly(2026, 5, 1),
        };
        _transactions.ListByPortfolioAsync(Arg.Any<TransactionQueryParameters>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Transaction>(new[] { transaction }, 37, 2, 20));

        PagedResult<TransactionDto> result = await _handler.Handle(
            new GetTransactionsQuery(), CancellationToken.None);

        result.TotalCount.Should().Be(37);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(20);
        result.Items.Should().ContainSingle();
        result.Items[0].Ticker.Should().Be("AAPL");
        result.Items[0].AssetName.Should().Be("Apple Inc.");
        result.Items[0].Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Handle_WhenNoPortfolioForUser_ThrowsNotFound()
    {
        _portfolios.GetByUserIdAsync(_userId, Arg.Any<CancellationToken>()).ReturnsNull();

        Func<Task> act = () => _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _transactions.DidNotReceive().ListByPortfolioAsync(
            Arg.Any<TransactionQueryParameters>(), Arg.Any<CancellationToken>());
    }
}
