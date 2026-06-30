using FluentValidation.TestHelper;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Features.Transactions.GetTransactions;
using PortfolioApp.Domain.Enums;

namespace PortfolioApp.UnitTests.Features.Transactions.GetTransactions;

public class GetTransactionsQueryValidatorTests
{
    private readonly GetTransactionsQueryValidator _validator = new();

    [Fact]
    public void Default_query_passes()
    {
        _validator.TestValidate(new GetTransactionsQuery()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_below_one_fails(int page)
    {
        _validator.TestValidate(new GetTransactionsQuery(Page: page))
            .ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(GetTransactionsQueryValidator.MaxPageSize + 1)]
    public void PageSize_out_of_bounds_fails(int pageSize)
    {
        _validator.TestValidate(new GetTransactionsQuery(PageSize: pageSize))
            .ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void From_after_to_fails()
    {
        var query = new GetTransactionsQuery(
            FromDate: new DateOnly(2026, 6, 1),
            ToDate: new DateOnly(2026, 1, 1));

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(nameof(GetTransactionsQuery.FromDate));
    }

    [Fact]
    public void Undefined_sort_field_fails()
    {
        _validator.TestValidate(new GetTransactionsQuery(SortBy: (TransactionSortField)99))
            .ShouldHaveValidationErrorFor(q => q.SortBy);
    }

    [Fact]
    public void Undefined_type_fails()
    {
        _validator.TestValidate(new GetTransactionsQuery(Type: (TransactionType)99))
            .ShouldHaveValidationErrorFor("Type.Value");
    }
}
