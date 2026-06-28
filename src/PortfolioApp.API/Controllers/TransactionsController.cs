using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PortfolioApp.API.Contracts;
using PortfolioApp.Application.Common.Models;
using PortfolioApp.Application.Features.Transactions.AddTransaction;
using PortfolioApp.Application.Features.Transactions.Common;
using PortfolioApp.Application.Features.Transactions.DeleteTransaction;
using PortfolioApp.Application.Features.Transactions.EditTransaction;
using PortfolioApp.Application.Features.Transactions.GetTransactionById;
using PortfolioApp.Application.Features.Transactions.GetTransactions;

namespace PortfolioApp.API.Controllers;

/// <summary>
/// CRUD endpoints for the authenticated user's portfolio transactions (FR-05, FR-06, FR-07).
/// Every action is scoped to the caller's own portfolio (FR-03) and requires a valid JWT
/// (NFR-04).
/// </summary>
[ApiController]
[Authorize]
[Route("transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Records a buy or sell against the caller's portfolio (FR-05).
    /// </summary>
    /// <response code="201">The transaction was created; the body carries the stored transaction.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    /// <response code="422">The request violated a business rule (e.g. selling more than is held).</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionDto>> Add(
        AddTransactionCommand command,
        CancellationToken cancellationToken)
    {
        TransactionDto result = await _sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Lists the caller's transactions with optional filtering, sorting, and paging (FR-07).
    /// </summary>
    /// <response code="200">A page of transactions plus the total matching count.</response>
    /// <response code="400">A query parameter failed validation.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<TransactionDto>>> List(
        [FromQuery] GetTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        PagedResult<TransactionDto> result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Fetches a single transaction owned by the caller (FR-06).
    /// </summary>
    /// <response code="200">The requested transaction.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    /// <response code="404">No such transaction exists for the caller.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        TransactionDto result = await _sender.Send(new GetTransactionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates a transaction owned by the caller (FR-06).
    /// </summary>
    /// <response code="200">The updated transaction.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    /// <response code="404">No such transaction exists for the caller.</response>
    /// <response code="422">The change violated a business rule (e.g. leaving a negative holding).</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionDto>> Update(
        Guid id,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EditTransactionCommand(
            id,
            request.Type,
            request.Quantity,
            request.PricePerUnit,
            request.Currency,
            request.TransactionDate);

        TransactionDto result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a transaction owned by the caller (FR-06).
    /// </summary>
    /// <response code="204">The transaction was deleted.</response>
    /// <response code="401">No valid bearer token was supplied.</response>
    /// <response code="404">No such transaction exists for the caller.</response>
    /// <response code="422">The deletion violated a business rule (e.g. leaving a negative holding).</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteTransactionCommand(id), cancellationToken);
        return NoContent();
    }
}
