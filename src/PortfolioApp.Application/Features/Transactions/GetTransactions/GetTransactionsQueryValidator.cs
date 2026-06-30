using FluentValidation;

namespace PortfolioApp.Application.Features.Transactions.GetTransactions;

/// <summary>
/// Input rules for <see cref="GetTransactionsQuery"/>: bounds the paging window and rejects
/// inverted date ranges or undefined enum values.
/// </summary>
public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    /// <summary>Upper bound on page size to keep a single response (and DB scan) bounded.</summary>
    public const int MaxPageSize = 100;

    public GetTransactionsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, MaxPageSize);

        RuleFor(query => query.SortBy)
            .IsInEnum();

        RuleFor(query => query.Type!.Value)
            .IsInEnum()
            .When(query => query.Type.HasValue);

        RuleFor(query => query)
            .Must(query => query.FromDate <= query.ToDate)
                .When(query => query.FromDate.HasValue && query.ToDate.HasValue)
                .WithName(nameof(GetTransactionsQuery.FromDate))
                .WithMessage("'From date' must not be after 'To date'.");
    }
}
