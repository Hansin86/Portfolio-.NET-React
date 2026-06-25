using MediatR;
using Microsoft.AspNetCore.Mvc;
using PortfolioApp.Application.Features.Auth.Common;
using PortfolioApp.Application.Features.Auth.Login;
using PortfolioApp.Application.Features.Auth.Register;

namespace PortfolioApp.API.Controllers;

/// <summary>
/// Authentication endpoints: registration and login (FR-01, FR-02). Both return a signed
/// JWT the client supplies as a bearer token on subsequent requests.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers a new user with an email and password and returns a JWT (FR-01).
    /// </summary>
    /// <response code="200">Registration succeeded; the body carries the JWT and user identity.</response>
    /// <response code="400">The email or password failed validation.</response>
    /// <response code="409">An account with the given email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        AuthResponseDto result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Authenticates a user by email and password and returns a JWT (FR-02).
    /// </summary>
    /// <response code="200">Login succeeded; the body carries the JWT and user identity.</response>
    /// <response code="400">The request was missing the email or password.</response>
    /// <response code="401">The email or password was incorrect.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        AuthResponseDto result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
