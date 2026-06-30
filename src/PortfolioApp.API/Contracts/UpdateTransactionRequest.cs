using PortfolioApp.Domain.Enums;

namespace PortfolioApp.API.Contracts;

/// <summary>
/// Request body for updating a transaction (FR-06). The transaction id comes from the route,
/// not the body, so it cannot be spoofed or mismatched.
/// </summary>
public record UpdateTransactionRequest(
    TransactionType Type,
    decimal Quantity,
    decimal PricePerUnit,
    string Currency,
    DateOnly TransactionDate);
